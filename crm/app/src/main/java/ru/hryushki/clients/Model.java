package ru.hryushki.clients;
import org.json.*;
import java.math.BigDecimal;
import java.time.*;
import java.time.format.*;
import java.util.*;

public final class Model {
 public static final String[] CHANNELS={"ВК","Авито","Другое"};
 public static final String[] ORDER_STATES={"Новый","Шьём","Готов к отправке","Отправлен","Доставлен","Отменён"};
 public static final String[] WISH_STATES={"Ожидает","Есть в наличии","Клиенту сообщили","Заказ создан","Отменено"};
 public static final DateTimeFormatter DATE=DateTimeFormatter.ofPattern("dd.MM.uuuu").withResolverStyle(ResolverStyle.STRICT);
 public static void require(boolean b,String s){if(!b)throw new IllegalArgumentException(s);}
 public static String s(JSONObject d,String k){return d.optString(k,"");}
 public static JSONObject copy(JSONObject d)throws JSONException{return new JSONObject(d.toString());}
 public static JSONArray rows(JSONObject d,String k){JSONArray a=d.optJSONArray(k);return a==null?new JSONArray():a;}
 public static List<JSONObject> list(JSONObject d,String k){List<JSONObject> a=new ArrayList<>();JSONArray r=rows(d,k);for(int i=0;i<r.length();i++)a.add(r.optJSONObject(i));return a;}
 public static LocalDate date(String s){try{return LocalDate.parse(s,DATE);}catch(Exception e){throw new IllegalArgumentException("Дата должна быть ДД.ММ.ГГГГ: "+s);}}
 public static String day(LocalDate d){return d.format(DATE);}public static String today(){return day(LocalDate.now());}
 public static String id(){return UUID.randomUUID().toString();}public static String now(){return Instant.now().toString();}
 public static BigDecimal number(String s){try{BigDecimal v=new BigDecimal(s.trim().replace(',','.').replace(" ","").replace("\u00a0","")).stripTrailingZeros();require(v.abs().compareTo(new BigDecimal("1000000000"))<=0,"Число слишком большое");return v;}catch(Exception e){throw new IllegalArgumentException("Некорректное число: "+s);}}
 public static BigDecimal n(JSONObject o,String key){String x=s(o,key);return number(x.isEmpty()?"0":x);}
 public static String value(BigDecimal n){return n.stripTrailingZeros().toPlainString();}
 public static JSONObject empty()throws JSONException{JSONObject d=new JSONObject();d.put("format","hryushki-clients");d.put("version",1);for(String k:new String[]{"clients","orders","wishes","audit"})d.put(k,new JSONArray());JSONObject settings=new JSONObject();settings.put("dispatchDays",3);settings.put("reminderHour",9);settings.put("deadlineHour",18);d.put("settings",settings);return d;}
 public static JSONObject find(JSONObject d,String kind,String id){for(JSONObject o:list(d,kind))if(s(o,"id").equals(id))return o;return null;}
 public static boolean openOrder(JSONObject o){return !Arrays.asList("Доставлен","Отменён").contains(s(o,"status"));}
 public static boolean needsShipping(JSONObject o){return !Arrays.asList("Отправлен","Доставлен","Отменён").contains(s(o,"status"))&&s(o,"shipped").isEmpty();}
 public static boolean openWish(JSONObject w){return !Arrays.asList("Заказ создан","Отменено").contains(s(w,"status"));}
 public static LocalDate due(JSONObject o){return date(s(o,"due"));}
 public static BigDecimal total(JSONObject o){BigDecimal result=BigDecimal.ZERO;for(JSONObject i:list(o,"items"))result=result.add(n(i,"qty").multiply(n(i,"price")));return result.add(n(o,"deliveryCost"));}
 public static JSONObject newOrder(JSONObject db,String clientId)throws JSONException{JSONObject c=find(db,"clients",clientId);require(c!=null,"Клиент не найден");require(!c.optBoolean("archived"),"Сначала верните клиента из архива");JSONObject o=new JSONObject();o.put("id",id());o.put("clientId",clientId);o.put("placed",today());o.put("due",day(LocalDate.now().plusDays(db.optJSONObject("settings").optInt("dispatchDays",3))));o.put("status",ORDER_STATES[0]);o.put("channel",s(c,"channel"));o.put("items",new JSONArray());o.put("createdAt",now());return o;}
 static void money(JSONObject o,String k){BigDecimal v=n(o,k);require(v.signum()>=0&&v.scale()<=2,"Суммы должны быть неотрицательными, до двух знаков после запятой");}
 static void validDate(JSONObject o,String k,boolean required){String v=s(o,k);if(required||!v.isEmpty())date(v);}
 public static void validate(JSONObject db)throws JSONException{
  require(s(db,"format").equals("hryushki-clients")&&db.optInt("version")==1,"Это не база клиентов «Хрюшкины подушки» формата 1");
  for(String key:new String[]{"clients","orders","wishes","audit"}){require(db.optJSONArray(key)!=null,"Отсутствует список "+key);for(JSONObject row:list(db,key))require(row!=null,"Повреждённая запись в "+key);}
  JSONObject settings=db.optJSONObject("settings");require(settings!=null,"Нет настроек");require(settings.optInt("dispatchDays",-1)>=1&&settings.optInt("dispatchDays")<=30,"Срок отправки: 1–30 дней");for(String k:new String[]{"reminderHour","deadlineHour"})require(settings.optInt(k,-1)>=0&&settings.optInt(k)<=23,"Час: от 0 до 23");
  Set<String> allIds=new HashSet<>();for(String key:new String[]{"clients","orders","wishes"})for(JSONObject o:list(db,key)){require(o!=null&&!s(o,"id").isEmpty()&&allIds.add(s(o,"id")),"Повторяющийся или пустой идентификатор");if(key.equals("clients")){require(!s(o,"name").trim().isEmpty(),"Введите имя клиента");require(Arrays.asList(CHANNELS).contains(s(o,"channel")),"Выберите канал");}}
  for(JSONObject o:list(db,"orders"))validateOrder(db,o);for(JSONObject w:list(db,"wishes")){require(find(db,"clients",s(w,"clientId"))!=null,"У пожелания нет клиента");require(!s(w,"product").trim().isEmpty(),"Опишите желаемое изделие");require(Arrays.asList(WISH_STATES).contains(s(w,"status")),"Неверный статус пожелания");require(n(w,"qty").signum()>0&&n(w,"qty").scale()<=0,"Количество пожелания — целое положительное");validDate(w,"remindOn",false);if(!s(w,"orderId").isEmpty()){JSONObject linked=find(db,"orders",s(w,"orderId"));require(linked!=null&&s(linked,"clientId").equals(s(w,"clientId")),"Потеряна связь пожелания с заказом");}}
 }
 public static void validateOrder(JSONObject db,JSONObject o)throws JSONException{
  require(find(db,"clients",s(o,"clientId"))!=null,"У заказа нет клиента");require(Arrays.asList(ORDER_STATES).contains(s(o,"status")),"Выберите статус заказа");require(Arrays.asList(CHANNELS).contains(s(o,"channel")),"Выберите канал заказа");
  validDate(o,"placed",true);validDate(o,"due",true);validDate(o,"shipped",false);validDate(o,"expected",false);validDate(o,"delivered",false);
  require(!date(s(o,"due")).isBefore(date(s(o,"placed"))),"Срок отправки раньше оформления");if(!s(o,"shipped").isEmpty())require(!date(s(o,"shipped")).isBefore(date(s(o,"placed"))),"Отправка раньше оформления");
  if(Arrays.asList("Отправлен","Доставлен").contains(s(o,"status")))require(!s(o,"shipped").isEmpty(),"Укажите фактическую дату отправки");
  if(needsShipping(o)==false&&!s(o,"shipped").isEmpty())require(Arrays.asList("Отправлен","Доставлен","Отменён").contains(s(o,"status")),"С датой отправки выберите статус «Отправлен» или «Доставлен»");
  if(s(o,"status").equals("Доставлен"))require(!s(o,"delivered").isEmpty(),"Укажите фактическую дату доставки");
  if(!s(o,"delivered").isEmpty()){require(s(o,"status").equals("Доставлен"),"Для фактической доставки выберите статус «Доставлен»");require(!s(o,"shipped").isEmpty()&&!date(s(o,"delivered")).isBefore(date(s(o,"shipped"))),"Доставка раньше отправки");}
  if(!s(o,"expected").isEmpty())require(!date(s(o,"expected")).isBefore(date(s(o,s(o,"shipped").isEmpty()?"placed":"shipped"))),"Ожидаемая доставка раньше оформления / отправки");
  require(o.optJSONArray("items")!=null&&rows(o,"items").length()>0,"Добавьте хотя бы одно изделие");for(JSONObject i:list(o,"items")){require(i!=null&&!s(i,"name").trim().isEmpty(),"Укажите название изделия");require(n(i,"qty").signum()>0&&n(i,"qty").scale()<=0,"Количество изделий — целое положительное");money(i,"price");}money(o,"deliveryCost");money(o,"prepaid");require(n(o,"prepaid").compareTo(total(o))<=0,"Предоплата больше суммы заказа");
 }
 public static void upsert(JSONObject db,String kind,JSONObject row)throws JSONException{JSONArray a=rows(db,kind);int index=-1;for(int i=0;i<a.length();i++)if(s(a.getJSONObject(i),"id").equals(s(row,"id")))index=i;JSONObject history=new JSONObject();history.put("at",now());history.put("kind",kind);history.put("id",s(row,"id"));history.put("action",index<0?"create":"edit");if(index>=0)history.put("before",copy(a.getJSONObject(index)));rows(db,"audit").put(history);row.put("updatedAt",now());if(index>=0)a.put(index,row);else{if(kind.equals("orders")){int max=0;for(JSONObject x:list(db,"orders"))max=Math.max(max,x.optInt("number"));row.put("number",max+1);}a.put(row);}db.put(kind,a);validate(db);}
 public static JSONObject convertWish(JSONObject db,String wishId)throws JSONException{JSONObject w=find(db,"wishes",wishId);require(w!=null,"Пожелание не найдено");if(!s(w,"orderId").isEmpty())return find(db,"orders",s(w,"orderId"));require(openWish(w),"Это пожелание закрыто");JSONObject o=newOrder(db,s(w,"clientId"));JSONObject item=new JSONObject();item.put("name",s(w,"product"));for(String k:new String[]{"sku","color","size"})item.put(k,s(w,k));item.put("qty",n(w,"qty"));item.put("price",0);rows(o,"items").put(item);o.put("note",s(w,"note"));o.put("wishId",wishId);o.put("pricesAgreed",false);upsert(db,"orders",o);JSONObject edited=copy(w);edited.put("orderId",s(o,"id"));edited.put("status","Заказ создан");upsert(db,"wishes",edited);return o;}
 public static class Event {
  public final String key,orderId,title,text;public final long at;public Event(String key,String orderId,String title,String text,long at){this.key=key;this.orderId=orderId;this.title=title;this.text=text;this.at=at;}
 }
 static long at(LocalDate d,int hour,ZoneId zone){return d.atTime(hour,0).atZone(zone).toInstant().toEpochMilli();}
 public static List<Event> events(JSONObject db,long now,ZoneId zone){List<Event> events=new ArrayList<>();JSONObject settings=db.optJSONObject("settings");int hour=settings.optInt("reminderHour",9);LocalDate today=Instant.ofEpochMilli(now).atZone(zone).toLocalDate();
  for(JSONObject o:list(db,"orders")){String id=s(o,"id"),due=s(o,"due"),number="Заказ №"+o.optInt("number");JSONObject c=find(db,"clients",s(o,"clientId"));String name=c==null?"":s(c,"name");
   if(needsShipping(o)){LocalDate day=date(due);if(!day.isBefore(today))events.add(new Event("ship:"+id+":"+due,id,"Пора отправить · "+number,name+" · отправка до "+due,at(day,hour,zone)));if(!day.minusDays(1).isBefore(today))events.add(new Event("early:"+id+":"+due,id,"Завтра отправка · "+number,name+" · проверьте готовность",at(day.minusDays(1),hour,zone)));if(day.isBefore(today))events.add(new Event("overdue:"+id+":"+today,id,"Просрочена отправка · "+number,name+" · срок был "+due,at(today,hour,zone)));}
   if(openOrder(o)&&!s(o,"expected").isEmpty())events.add(new Event("delivery:"+id+":"+s(o,"expected"),id,"Проверить доставку · "+number,name+" · ожидаемая дата "+s(o,"expected"),at(date(s(o,"expected")),hour,zone)));
  }
  for(JSONObject w:list(db,"wishes"))if(openWish(w)&&!s(w,"remindOn").isEmpty()){JSONObject c=find(db,"clients",s(w,"clientId"));events.add(new Event("wish:"+s(w,"id")+":"+s(w,"remindOn"),"","Пожелание: связаться с клиентом",(c==null?"":s(c,"name"))+" · "+s(w,"product")+" · "+s(w,"color"),at(date(s(w,"remindOn")),hour,zone)));}
  return events;
 }
}
