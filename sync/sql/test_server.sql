\set ON_ERROR_STOP on
insert into public.hry_sync_accounts(user_id) select id from auth.users;
set role authenticated;
select set_config('request.jwt.claim.sub','11111111-1111-1111-1111-111111111111',false);
do $$declare r jsonb;begin
 r:=public.hry_sync_exchange('[{"kind":"product","key":"p","base_version":0,"data":{"_syncId":"p","name":"Лежанка","HomePrice":680}}]');
 assert (r->>'ok')::boolean;assert r->'records'->0->>'version'='1';
 r:=public.hry_sync_exchange('[{"kind":"product","key":"p","base_version":0,"data":{"_syncId":"p","name":"Лежанка","HomePrice":680}}]');assert (r->>'ok')::boolean;assert r->'records'->0->>'version'='1';
 r:=public.hry_sync_exchange('[{"kind":"product","key":"p","base_version":0,"data":{"_syncId":"p","name":"Лежанка","HomePrice":700}}]');assert not (r->>'ok')::boolean;assert jsonb_array_length(r->'conflicts')=1;
 r:=public.hry_sync_exchange('[{"kind":"product","key":"p","base_version":1,"data":{"_syncId":"p","name":"Лежанка","HomePrice":710}}]');assert (r->>'ok')::boolean;assert r->'records'->0->>'version'='2';
end$$;
-- Два клиента не могут продать одну оставшуюся вещь: весь пакет откатывается.
select public.hry_sync_exchange('[{"kind":"operation","key":"make","base_version":0,"data":{"_syncId":"make","type":"Производство","date":"03.11.2026","warehouse":"Дома","item":"Лежанка","qty":1,"amount":0}}]');
select public.hry_sync_exchange('[{"kind":"operation","key":"sale-a","base_version":0,"data":{"_syncId":"sale-a","type":"Продажа","date":"03.11.2026","warehouse":"Дома","item":"Лежанка","qty":1,"amount":680,"paid":680}}]');
do $$declare blocked boolean:=false;r jsonb;begin
 begin perform public.hry_sync_exchange('[{"kind":"operation","key":"sale-b","base_version":0,"data":{"_syncId":"sale-b","type":"Продажа","date":"03.11.2026","warehouse":"Дома","item":"Лежанка","qty":1,"amount":680,"paid":680}}]');exception when others then blocked:=true;end;
 assert blocked; r:=public.hry_sync_exchange('[]');assert jsonb_array_length(r->'records')=3;
end$$;
select set_config('request.jwt.claim.sub','22222222-2222-2222-2222-222222222222',false);
do $$declare r jsonb;begin r:=public.hry_sync_exchange('[]');assert jsonb_array_length(r->'records')=0;end$$;
reset role;
do $$begin assert not has_function_privilege('anon','public.hry_sync_exchange(jsonb)','execute');assert not has_table_privilege('authenticated','public.hry_sync_records','insert');assert not has_table_privilege('authenticated','public.hry_sync_records','select');assert (select count(*) from public.hry_sync_history)=1;end$$;
select 'PASS SQL isolation, atomicity, conflicts, retry deduplication and stock checks';
