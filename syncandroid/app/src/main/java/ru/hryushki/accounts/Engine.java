package ru.hryushki.accounts;

import org.json.*;
import java.math.BigDecimal;
import java.text.*;
import java.util.*;

/** JSON v2 accounting rules shared with Windows 3.0. All money uses decimals. */
public final class Engine {
 public static final String HOME="Дома", PARTNER="Зверёк Аморе", MATERIALS="Материалы";
 public static final String[] TYPES={"Продажа","Расход","Поступление","Производство","Передача партнёру","Возврат от партнёра","Корректировка склада","Выплата партнёра","Вложение владельца","Вывод владельцу","Списание","Покупка материалов","Расход материалов"};
 public static final BigDecimal ZERO=BigDecimal.ZERO, LIMIT=new BigDecimal("1000000000");
 public static JSONObject copy(JSONObject o)throws JSONException{return new JSONObject(o.toString());}
 public static JSONArray rows(JSONObject o,String key){JSONArray a=o.optJSONArray(key);return a==null?new JSONArray():a;}
 public static String s(JSONObject o,String key,String def){return o.has(key)&&!o.isNull(key)?o.optString(key,def):def;}
 public static BigDecimal n(JSONObject o,String key){return n(o,key,ZERO);}
 public static BigDecimal n(JSONObject o,String key,BigDecimal def){if(!o.has(key)||o.isNull(key))return def;return decimal(o.optString(key));}
 public static BigDecimal decimal(String s){try{return new BigDecimal(s.trim().replace(',','.').replace(" ","").replace("\u00a0","")).stripTrailingZeros();}catch(Exception e){throw new IllegalArgumentException("Некорректное число: "+s);}}
 public static long date(String s){if("Без даты".equals(s))return Long.MIN_VALUE;for(String pattern:new String[]{"dd.MM.yyyy","yyyy-MM-dd"}){SimpleDateFormat f=new SimpleDateFormat(pattern,Locale.ROOT);f.setLenient(false);ParsePosition p=new ParsePosition(0);Date d=f.parse(s,p);if(d!=null&&p.getIndex()==s.length())return d.getTime();}throw new IllegalArgumentException("Дата: ДД.ММ.ГГГГ или ГГГГ-ММ-ДД");}
 public static boolean active(JSONObject o){return !o.optBoolean("_void",false);}
 public static JSONObject empty()throws JSONException{JSONObject d=new JSONObject();d.put("version",2);d.put("warehouses",new JSONArray(Arrays.asList(HOME,PARTNER)));d.put("operations",new JSONArray());d.put("products",new JSONArray());d.put("materials",new JSONArray());return d;}
 public static void require(boolean ok,String message){if(!ok)throw new IllegalArgumentException(message);}
 public static void validate(JSONObject d)throws JSONException{
  require(d.optInt("version",2)==2,"Поддерживается база JSON формата 2 (ПК 2.1 / 3.0)");
  require(d.optJSONArray("operations")!=null,"В базе нет списка operations");Set<String> ids=new HashSet<>();
  for(String key:new String[]{"operations","products","materials","_audit"}){if(d.has(key))require(d.optJSONArray(key)!=null,"Поле "+key+" должно быть списком");JSONArray a=rows(d,key);for(int i=0;i<a.length();i++)require(a.optJSONObject(i)!=null,"Повреждённая запись в "+key);}
  for(JSONObject o:list(rows(d,"operations"))){BigDecimal id=n(o,"id");require(id.signum()>0&&id.scale()<=0&&ids.add(id.toPlainString()),"Неверный или повторный номер операции");date(s(o,"date",""));for(String k:new String[]{"amount","qty","paid","fee","_cost"})if(o.has(k))require(n(o,k).abs().compareTo(LIMIT)<=0,"Слишком большое число в операции №"+id);}
  for(String key:new String[]{"products","materials"})for(JSONObject p:list(rows(d,key))){require(!s(p,"name","").trim().isEmpty(),"Пустое название в "+key);for(String k:new String[]{"HomePrice","PartnerPrice","Cost","qty","price"})if(p.has(k))n(p,k);}
 }
 public static List<JSONObject> list(JSONArray a){List<JSONObject> l=new ArrayList<>();for(int i=0;i<a.length();i++)if(a.optJSONObject(i)!=null)l.add(a.optJSONObject(i));return l;}
 public static String key(String wh,String item){return wh+"\u001f"+item;}
 public static class Totals {
  public BigDecimal revenue=ZERO,other=ZERO,expense=ZERO,payouts=ZERO,cash=ZERO,cogs=ZERO,debt=ZERO;
  public int sales,unknown;public Map<String,BigDecimal> stock=new TreeMap<>(),minimum=new TreeMap<>(),debts=new TreeMap<>();public List<String> warnings=new ArrayList<>();
  public BigDecimal result(){return revenue.add(other).subtract(expense).subtract(cogs);}
 }
 static void add(Map<String,BigDecimal> m,String k,BigDecimal n){m.put(k,m.getOrDefault(k,ZERO).add(n));}
 static void stock(Totals t,String wh,String item,BigDecimal q){String k=key(wh,item);add(t.stock,k,q);t.minimum.put(k,t.minimum.getOrDefault(k,ZERO).min(t.stock.get(k)));}
 static void debt(Totals t,String who,BigDecimal a){add(t.debts,who.trim().isEmpty()?PARTNER:who,a);}
 public static Totals calculate(JSONObject d,Long from,Long until){Totals t=new Totals();for(JSONObject m:list(rows(d,"materials")))stock(t,MATERIALS,s(m,"name",""),n(m,"qty"));List<JSONObject> ops=list(rows(d,"operations"));ops.sort((a,b)->{int c=Long.compare(date(s(a,"date","")),date(s(b,"date","")));return c==0?n(a,"id").compareTo(n(b,"id")):c;});int undated=0;
  for(JSONObject o:ops){if(!active(o))continue;long day=date(s(o,"date",""));if(day==Long.MIN_VALUE)undated++;if(until!=null&&day>until)continue;boolean period=from==null||day>=from;
   String type=s(o,"type",""),wh=s(o,"warehouse",HOME),item=s(o,"item",""),who=s(o,"counterparty",wh);BigDecimal q=n(o,"qty"),a=n(o,"amount"),fee=n(o,"fee");
   boolean payout=type.equals("Выплата партнёра")||(type.equals("Поступление")&&s(o,"category","").toLowerCase(Locale.ROOT).contains("партн"));
   switch(type){case "Передача партнёру":stock(t,HOME,item,q.negate());stock(t,wh.equals(HOME)?PARTNER:wh,item,q);break;case "Возврат от партнёра":stock(t,wh.equals(HOME)?PARTNER:wh,item,q.negate());stock(t,HOME,item,q);break;case "Производство":case "Корректировка склада":stock(t,wh,item,q);break;case "Списание":stock(t,wh,item,q.negate());break;case "Покупка материалов":stock(t,MATERIALS,item,q);break;case "Расход материалов":stock(t,MATERIALS,item,q.negate());break;}
   if(type.equals("Продажа")){stock(t,wh,item,q.negate());BigDecimal paid=n(o,"paid",wh.equals(HOME)?a.subtract(fee):ZERO);debt(t,who,a.subtract(fee).subtract(paid));t.cash=t.cash.add(paid);if(period){t.revenue=t.revenue.add(a);t.expense=t.expense.add(fee);t.sales++;if(o.has("_cost"))t.cogs=t.cogs.add(n(o,"_cost"));else t.unknown++;}}
   else if(payout){debt(t,who,a.negate());t.cash=t.cash.add(a);if(period)t.payouts=t.payouts.add(a);}
   else switch(type){case "Поступление":t.cash=t.cash.add(a);if(period)t.other=t.other.add(a);break;case "Расход":t.cash=t.cash.subtract(a);if(period)t.expense=t.expense.add(a);break;case "Покупка материалов":case "Вывод владельцу":t.cash=t.cash.subtract(a);break;case "Вложение владельца":t.cash=t.cash.add(a);break;case "Списание":if(period)t.expense=t.expense.add(a);break;}
   if(!Arrays.asList(TYPES).contains(type))t.warnings.add("Неизвестный тип: "+type+" (№"+s(o,"id","")+")");
  }
  if(undated>0)t.warnings.add(undated+" записей без даты: учтены как начальные, исключены из отчётов за период");
  for(Map.Entry<String,BigDecimal> e:t.stock.entrySet())if(e.getValue().signum()<0)t.warnings.add("Нужно сверить остаток: "+e.getKey().replace('\u001f','/')+" = "+e.getValue().toPlainString());
  for(Map.Entry<String,BigDecimal> e:t.debts.entrySet()){t.debt=t.debt.add(e.getValue());if(e.getValue().signum()<0)t.warnings.add("Переплата: "+e.getKey());}return t;
 }
 public static void validateOperation(JSONObject db,JSONObject o)throws JSONException{
  String type=s(o,"type","");require(Arrays.asList(TYPES).contains(type),"Выберите тип операции");date(s(o,"date",""));BigDecimal a=n(o,"amount"),q=n(o,"qty"),fee=n(o,"fee"),paid=n(o,"paid");
  require(a.signum()>=0&&a.scale()<=2&&a.compareTo(LIMIT)<=0,"Сумма: от 0 до 1 млрд, не более двух знаков");require(q.abs().compareTo(LIMIT)<=0&&q.scale()<=3,"Количество: не более трёх знаков после запятой");
  boolean inventory=Arrays.asList("Продажа","Производство","Передача партнёру","Возврат от партнёра","Корректировка склада","Списание","Покупка материалов","Расход материалов").contains(type);
  if(inventory)require(!s(o,"item","").trim().isEmpty()&&q.signum()!=0&&(type.equals("Корректировка склада")||q.signum()>0),"Укажите название и положительное количество; корректировка допускает минус");
  if(type.equals("Продажа")){require(fee.signum()>=0&&paid.signum()>=0&&fee.compareTo(a)<=0&&paid.compareTo(a.subtract(fee))<=0&&fee.scale()<=2&&paid.scale()<=2,"Комиссия и оплата: до двух знаков, вместе не больше суммы продажи");if(o.has("_cost"))require(n(o,"_cost").signum()>=0&&n(o,"_cost").scale()<=2,"Некорректная себестоимость");if(s(o,"warehouse",HOME).equals(HOME)&&paid.compareTo(a.subtract(fee))<0)require(!s(o,"counterparty","").trim().isEmpty(),"Укажите покупателя, который должен доплатить");}
  if(type.equals("Продажа")||type.equals("Выплата партнёра"))require(a.signum()>0,"Укажите сумму больше нуля");
  if(type.equals("Покупка материалов")||type.equals("Расход материалов"))require(find(db,"materials",s(o,"item",""))!=null,"Сначала добавьте карточку материала");
 }
 public static JSONObject find(JSONObject d,String key,String name){for(JSONObject p:list(rows(d,key)))if(s(p,"name","").equals(name))return p;return null;}
 public static void validateChange(JSONObject before,JSONObject after)throws JSONException{validate(after);Totals b=calculate(before,null,null),a=calculate(after,null,null);for(Map.Entry<String,BigDecimal> p:a.minimum.entrySet())require(p.getValue().compareTo(b.minimum.getOrDefault(p.getKey(),ZERO))>=0,"Нехватка на складе: "+p.getKey().replace('\u001f','/')+". Внесите начальный остаток или производство");for(Map.Entry<String,BigDecimal> p:a.stock.entrySet())require(p.getValue().compareTo(b.stock.getOrDefault(p.getKey(),ZERO).min(ZERO))>=0,"Недостаточный остаток: "+p.getKey().replace('\u001f','/'));for(Map.Entry<String,BigDecimal> p:a.debts.entrySet())require(p.getValue().compareTo(b.debts.getOrDefault(p.getKey(),ZERO).min(ZERO))>=0,"Оплата превышает долг: "+p.getKey());}
 public static String now(){return new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSSXXX",Locale.ROOT).format(new Date());}
 public static JSONObject operationChange(JSONObject db,JSONObject o,long id)throws JSONException{validateOperation(db,o);JSONObject next=copy(db);JSONArray ops=rows(next,"operations");if(id==0){long max=0;for(JSONObject x:list(ops))max=Math.max(max,x.optLong("id"));o.put("id",max+1);ops.put(o);}else{int index=-1;for(int i=0;i<ops.length();i++)if(ops.getJSONObject(i).optLong("id")==id)index=i;require(index>=0,"Операция не найдена");require(active(ops.getJSONObject(index)),"Отменённую запись нельзя редактировать");audit(next,"edit",ops.getJSONObject(index));o.put("id",id);ops.put(index,o);}next.put("operations",ops);validateChange(db,next);return next;}
 public static void audit(JSONObject db,String action,JSONObject before)throws JSONException{JSONArray h=rows(db,"_audit");JSONObject e=new JSONObject();e.put("at",now());e.put("action",action);e.put("before",copy(before));h.put(e);db.put("_audit",h);}
 public static JSONObject voidOperation(JSONObject db,long id,String reason)throws JSONException{require(!reason.trim().isEmpty(),"Укажите причину отмены");JSONObject next=copy(db);for(JSONObject o:list(rows(next,"operations")))if(o.optLong("id")==id){require(active(o),"Запись уже отменена");audit(next,"void",o);o.put("_void",true);o.put("_voidReason",reason.trim());o.put("_voidAt",now());validateChange(db,next);return next;}throw new IllegalArgumentException("Запись не найдена");}
 public static JSONObject catalogChange(JSONObject db,boolean material,String oldName,JSONObject card)throws JSONException{JSONObject next=copy(db);String key=material?"materials":"products",name=s(card,"name","").trim(),sku=s(card,"sku","").trim();require(!name.isEmpty(),"Введите название");JSONArray a=rows(next,key);int index=-1;for(int i=0;i<a.length();i++){JSONObject p=a.getJSONObject(i);if(s(p,"name","").equals(oldName))index=i;else{require(!s(p,"name","").equalsIgnoreCase(name),"Название уже существует");if(!material&&!sku.isEmpty())require(!sku.equalsIgnoreCase(s(p,"sku","")),"Артикул уже используется");}}
  if(oldName!=null&&!oldName.equals(name))for(JSONObject o:list(rows(next,"operations")))require(!s(o,"item","").equals(oldName),"У карточки есть операции: создайте новый вариант с другим названием");
  for(String k:material?new String[]{"price"}:new String[]{"HomePrice","PartnerPrice","Cost"})require(n(card,k).signum()>=0&&n(card,k).scale()<=2&&n(card,k).compareTo(LIMIT)<=0,"Цена: неотрицательная, до двух знаков");if(material)require(n(card,"qty").signum()>=0&&n(card,"qty").scale()<=3,"Начальный остаток: неотрицательный, до трёх знаков");
  if(index<0)a.put(card);else a.put(index,card);next.put(key,a);audit(next,material?"material":"product",oldName==null?new JSONObject():find(db,key,oldName));validateChange(db,next);return next;
 }
}
