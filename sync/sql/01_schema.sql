-- Хрюшкины подушки: изолированный тест синхронизации, формат 1.
-- Выполнять в Supabase SQL Editor под владельцем проекта.
begin;
create table if not exists public.hry_sync_accounts (
 user_id uuid primary key references auth.users(id) on delete cascade,
 enabled boolean not null default true
);
create table if not exists public.hry_sync_records (
 owner_id uuid not null references public.hry_sync_accounts(user_id),
 kind text not null check(kind in ('operation','product','material')),
 record_key text not null check(length(record_key) between 1 and 160),
 version bigint not null check(version>0),
 data jsonb not null check(jsonb_typeof(data)='object'),
 created_seq bigint generated always as identity,
 primary key(owner_id,kind,record_key)
);
create table if not exists public.hry_sync_history (
 owner_id uuid not null references public.hry_sync_accounts(user_id),
 kind text not null, record_key text not null, version bigint not null,
 data jsonb not null, changed_at timestamptz not null default now()
);
alter table public.hry_sync_accounts enable row level security;
alter table public.hry_sync_records enable row level security;
alter table public.hry_sync_history enable row level security;
revoke all on public.hry_sync_accounts,public.hry_sync_records,public.hry_sync_history from anon,authenticated;
-- Все записи проходят через атомарную функцию; обхода проверки версий нет.
create or replace function public.hry_sync_balances(p_owner uuid)
returns table(bucket text, balance numeric)
language sql stable security definer set search_path='' as $$
with ops as (
 select data d from public.hry_sync_records where owner_id=p_owner and kind='operation' and coalesce((data->>'_void')::boolean,false)=false
), effects as (
 select 'stock:'||coalesce(d->>'warehouse','Дома')||':'||(d->>'item') k,
 case when d->>'type' in ('Продажа','Списание') then -(d->>'qty')::numeric else (d->>'qty')::numeric end v
 from ops where d->>'type' in ('Продажа','Списание','Производство','Корректировка склада')
 union all select 'stock:Дома:'||(d->>'item'),-(d->>'qty')::numeric from ops where d->>'type'='Передача партнёру'
 union all select 'stock:'||case when coalesce(d->>'warehouse','Дома')='Дома' then 'Зверёк Аморе' else d->>'warehouse' end||':'||(d->>'item'),(d->>'qty')::numeric from ops where d->>'type'='Передача партнёру'
 union all select 'stock:Дома:'||(d->>'item'),(d->>'qty')::numeric from ops where d->>'type'='Возврат от партнёра'
 union all select 'stock:'||case when coalesce(d->>'warehouse','Дома')='Дома' then 'Зверёк Аморе' else d->>'warehouse' end||':'||(d->>'item'),-(d->>'qty')::numeric from ops where d->>'type'='Возврат от партнёра'
 union all select 'stock:Материалы:'||(d->>'item'),case when d->>'type'='Расход материалов' then -(d->>'qty')::numeric else (d->>'qty')::numeric end from ops where d->>'type' in ('Покупка материалов','Расход материалов')
 union all select 'stock:Материалы:'||(data->>'name'),coalesce((data->>'qty')::numeric,0) from public.hry_sync_records where owner_id=p_owner and kind='material'
 union all select 'debt:'||coalesce(nullif(d->>'counterparty',''),nullif(d->>'warehouse',''),'Зверёк Аморе'),
 (d->>'amount')::numeric-coalesce((d->>'fee')::numeric,0)-coalesce((d->>'paid')::numeric,case when coalesce(d->>'warehouse','Дома')='Дома' then (d->>'amount')::numeric-coalesce((d->>'fee')::numeric,0) else 0 end)
 from ops where d->>'type'='Продажа'
 union all select 'debt:'||coalesce(nullif(d->>'counterparty',''),nullif(d->>'warehouse',''),'Зверёк Аморе'),-(d->>'amount')::numeric from ops where d->>'type'='Выплата партнёра' or (d->>'type'='Поступление' and lower(coalesce(d->>'category','')) like '%партн%')
) select k,sum(v) from effects group by k
$$;
revoke all on function public.hry_sync_balances(uuid) from public,anon,authenticated;

create or replace function public.hry_sync_exchange(p_changes jsonb default '[]'::jsonb)
returns jsonb language plpgsql security definer set search_path='' as $$
declare
 uid uuid:=auth.uid(); x jsonb; old public.hry_sync_records%rowtype;
 conflicts jsonb:='[]'; snapshot jsonb; before_bal jsonb; bad text;
begin
 if uid is null then raise exception 'Требуется вход' using errcode='42501'; end if;
 if not exists(select 1 from public.hry_sync_accounts a where a.user_id=uid and a.enabled) then
  raise exception 'Аккаунт не допущен к тесту. Выполните 02_allow_user.sql' using errcode='42501';
 end if;
 if jsonb_typeof(p_changes)<>'array' or jsonb_array_length(p_changes)>400 then raise exception 'За один обмен допускается до 400 изменений'; end if;
 perform pg_advisory_xact_lock(hashtextextended(uid::text,0));
 if (select count(*) from public.hry_sync_records where owner_id=uid)>5000 then raise exception 'Лимит теста: 5000 записей'; end if;
 if exists(select 1 from jsonb_array_elements(p_changes) q group by q->>'kind',q->>'key' having count(*)>1) then raise exception 'Повтор записи в пакете'; end if;
 for x in select value from jsonb_array_elements(p_changes) loop
  if x->>'kind' not in ('operation','product','material') or coalesce(length(x->>'key'),0) not between 1 and 160
   or jsonb_typeof(x->'data')<>'object' or pg_column_size(x->'data')>262144
   or coalesce(x->'data'->>'_syncId','')<>x->>'key' or coalesce((x->>'base_version')::bigint,-1)<0 then raise exception 'Неверный формат записи'; end if;
  if x->>'kind' in ('product','material') and btrim(coalesce(x->'data'->>'name',''))='' then raise exception 'Пустое название'; end if;
  if x->>'kind'='operation' then
   if coalesce(x->'data'->>'type','') not in ('Продажа','Расход','Поступление','Производство','Передача партнёру','Возврат от партнёра','Корректировка склада','Выплата партнёра','Вложение владельца','Вывод владельцу','Списание','Покупка материалов','Расход материалов') then raise exception 'Неверный тип операции'; end if;
   if not (x->'data' ? 'date') or not(x->'data' ? 'qty') or not(x->'data' ? 'amount') then raise exception 'Неполная операция'; end if;
   if abs((x->'data'->>'qty')::numeric)>1000000000 or (x->'data'->>'amount')::numeric not between 0 and 1000000000 then raise exception 'Неверная сумма / количество'; end if;
   if x->'data'->>'type'='Продажа' and (coalesce((x->'data'->>'fee')::numeric,0)<0 or coalesce((x->'data'->>'paid')::numeric,0)<0 or coalesce((x->'data'->>'paid')::numeric,0)+coalesce((x->'data'->>'fee')::numeric,0)>(x->'data'->>'amount')::numeric) then raise exception 'Оплата и комиссия превышают сумму'; end if;
  end if;
  select * into old from public.hry_sync_records where owner_id=uid and kind=x->>'kind' and record_key=x->>'key';
  if coalesce(old.version,0)<>(x->>'base_version')::bigint and (old.data is null or old.data<>x->'data') then
   conflicts:=conflicts||jsonb_build_array(jsonb_build_object('kind',x->>'kind','key',x->>'key','remote_version',coalesce(old.version,0),'remote',old.data,'local',x->'data'));
  end if;
 end loop;
 if jsonb_array_length(conflicts)=0 then
  select coalesce(jsonb_object_agg(bucket,balance),'{}') into before_bal from public.hry_sync_balances(uid);
  for x in select value from jsonb_array_elements(p_changes) loop
   select * into old from public.hry_sync_records where owner_id=uid and kind=x->>'kind' and record_key=x->>'key';
   if old.data is not null and old.data=x->'data' then continue; end if;
   if old.data is not null then insert into public.hry_sync_history(owner_id,kind,record_key,version,data) values(uid,old.kind,old.record_key,old.version,old.data); end if;
   insert into public.hry_sync_records(owner_id,kind,record_key,version,data) values(uid,x->>'kind',x->>'key',coalesce(old.version,0)+1,x->'data')
   on conflict(owner_id,kind,record_key) do update set data=excluded.data,version=excluded.version;
  end loop;
  select b.bucket into bad from public.hry_sync_balances(uid) b where b.balance<least(0,coalesce((before_bal->>b.bucket)::numeric,0)) limit 1;
  if bad is not null then raise exception 'Обмен остановлен: нехватка остатка или переплата (%). Проверьте операции на обоих устройствах',bad; end if;
  if exists(select 1 from public.hry_sync_records where owner_id=uid and kind in ('product','material') group by kind,lower(data->>'name') having count(*)>1) then raise exception 'Одинаковые названия карточек: проверьте конфликт'; end if;
  if exists(select 1 from public.hry_sync_records where owner_id=uid and kind='product' and coalesce(data->>'sku','')<>'' group by lower(data->>'sku') having count(*)>1) then raise exception 'Повторяющийся артикул'; end if;
 end if;
 select coalesce(jsonb_agg(jsonb_build_object('kind',kind,'key',record_key,'version',version,'data',data) order by created_seq),'[]') into snapshot from public.hry_sync_records where owner_id=uid;
 return jsonb_build_object('ok',jsonb_array_length(conflicts)=0,'owner',uid,'records',snapshot,'conflicts',conflicts);
end $$;
revoke all on function public.hry_sync_exchange(jsonb) from public,anon;
grant execute on function public.hry_sync_exchange(jsonb) to authenticated;
commit;
