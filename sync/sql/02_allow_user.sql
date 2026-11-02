-- Сначала Authentication → Users → Add user → Create new user.
-- Укажите СВОЙ email тестового аккаунта в единственной строке ниже.
do $$
declare chosen text:='ВСТАВЬТЕ_EMAIL_ТЕСТОВОГО_АККАУНТА'; u uuid;
begin
 select id into u from auth.users where lower(email)=lower(chosen);
 if u is null then raise exception 'Пользователь не найден. Создайте его и впишите email в chosen'; end if;
 insert into public.hry_sync_accounts(user_id) values(u) on conflict(user_id) do update set enabled=true;
end $$;
