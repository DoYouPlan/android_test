using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;
using System.Security.Cryptography;

namespace Hryushki {
public static class J {
 public static JavaScriptSerializer Serializer(){return new JavaScriptSerializer{MaxJsonLength=32*1024*1024,RecursionLimit=64};}
 public static Dictionary<string,object> Parse(string s){return Serializer().Deserialize<Dictionary<string,object>>(s.TrimStart('\ufeff'));}
 public static string Dump(object o){return Serializer().Serialize(o);}
 public static string S(Dictionary<string,object> d,string k,string def=""){object o;return d.TryGetValue(k,out o)&&o!=null?Convert.ToString(o,CultureInfo.InvariantCulture):def;}
 public static decimal N(Dictionary<string,object> d,string k,decimal def=0){decimal n;return Decimal.TryParse(S(d,k),NumberStyles.Number,CultureInfo.InvariantCulture,out n)?n:def;}
 public static bool B(Dictionary<string,object> d,string k){return S(d,k).ToLowerInvariant()=="true";}
 public static List<Dictionary<string,object>> Rows(Dictionary<string,object> d,string key){object o;if(!d.TryGetValue(key,out o)||o==null)return new List<Dictionary<string,object>>();var e=o as IEnumerable;if(e==null||o is string)throw new Exception("Поле "+key+" должно быть списком");var r=new List<Dictionary<string,object>>();foreach(object x in e){var row=x as Dictionary<string,object>;if(row==null)throw new Exception("Повреждённая запись в "+key);r.Add(row);}return r;}
 public static Dictionary<string,object> Copy(Dictionary<string,object> x){return Parse(Dump(x));}
 public static DateTime Date(string s){if(s=="Без даты")return DateTime.MinValue;DateTime d;if(!DateTime.TryParseExact(s,new[]{"dd.MM.yyyy","yyyy-MM-dd"},CultureInfo.InvariantCulture,DateTimeStyles.None,out d))throw new Exception("Дата должна иметь формат ДД.ММ.ГГГГ");return d;}
 public static string Money(decimal n){return n.ToString("N2",CultureInfo.GetCultureInfo("ru-RU"))+" ₽";}
 public static Dictionary<string,object> Empty(){return new Dictionary<string,object>{{"version",2},{"warehouses",new[]{"Дома","Зверёк Аморе"}},{"operations",new object[0]},{"products",new object[0]},{"materials",new object[0]}};}
 public static void Validate(Dictionary<string,object> d){
  if(N(d,"version",2)!=2)throw new Exception("Поддерживается JSON-база формата 2 (версии 2.0 / 2.1 / 3.0)");
  if(!d.ContainsKey("operations"))throw new Exception("В файле нет списка operations");
  var ids=new HashSet<decimal>();foreach(var o in Rows(d,"operations")){if(!ids.Add(N(o,"id"))||N(o,"id")<=0)throw new Exception("Повторяющийся или неверный номер операции");Date(S(o,"date"));foreach(string k in new[]{"amount","qty","paid","fee","_cost"}){if(o.ContainsKey(k)){decimal v;if(!Decimal.TryParse(S(o,k),NumberStyles.Number,CultureInfo.InvariantCulture,out v)||Math.Abs(v)>1000000000)throw new Exception("Некорректное число в операции №"+S(o,"id"));}}}
  Rows(d,"products");Rows(d,"materials");
 }
}
public class Totals {
 public decimal Revenue,OtherIncome,Expense,Payouts,CashFlow,Cogs,Debt;
 public int Sales,UnknownCost;
 public Dictionary<string,decimal> Stock=new Dictionary<string,decimal>();
 public Dictionary<string,decimal> Minimum=new Dictionary<string,decimal>();
 public Dictionary<string,decimal> Debts=new Dictionary<string,decimal>();
 public List<string> Warnings=new List<string>();
 public decimal Result{get{return Revenue+OtherIncome-Expense-Cogs;}}
}
public static class Engine {
 public const string Home="Дома",Partner="Зверёк Аморе";
 public static readonly string[] Types={"Продажа","Расход","Поступление","Производство","Передача партнёру","Возврат от партнёра","Корректировка склада","Выплата партнёра","Вложение владельца","Вывод владельцу","Списание","Покупка материалов","Расход материалов"};
 public static string Key(string wh,string item){return wh+"\u001f"+item;}
 public static string[] Warehouses(Dictionary<string,object> d){object o;var a=new List<string>{Home,Partner};if(d.TryGetValue("warehouses",out o)&&o is IEnumerable)foreach(var x in (IEnumerable)o){string s=Convert.ToString(x);if(!String.IsNullOrWhiteSpace(s)&&!a.Contains(s))a.Add(s);}return a.ToArray();}
 public static bool Active(Dictionary<string,object> o){return !J.B(o,"_void");}
 static void Stock(Totals t,string wh,string item,decimal q){string k=Key(wh,item);if(!t.Stock.ContainsKey(k)){t.Stock[k]=0;t.Minimum[k]=0;}t.Stock[k]+=q;t.Minimum[k]=Math.Min(t.Minimum[k],t.Stock[k]);}
 static void Debt(Totals t,string who,decimal a){if(String.IsNullOrWhiteSpace(who))who=Partner;if(!t.Debts.ContainsKey(who))t.Debts[who]=0;t.Debts[who]+=a;}
 public static Totals Calculate(Dictionary<string,object> d,DateTime? from=null,DateTime? until=null){
  var t=new Totals();foreach(var m in J.Rows(d,"materials"))Stock(t,"Материалы",J.S(m,"name"),J.N(m,"qty"));var allowed=new HashSet<string>(Types);
  foreach(var o in J.Rows(d,"operations").Where(Active).OrderBy(o=>J.Date(J.S(o,"date"))).ThenBy(o=>J.N(o,"id"))){
   DateTime day=J.Date(J.S(o,"date"));if(until.HasValue&&day>until.Value.Date)continue;
   bool period=!from.HasValue||day>=from.Value.Date;
   string type=J.S(o,"type"),wh=J.S(o,"warehouse",Home),item=J.S(o,"item"),who=J.S(o,"counterparty",wh);
   decimal q=J.N(o,"qty"),a=J.N(o,"amount"),fee=J.N(o,"fee");
   bool partnerIncome=type=="Выплата партнёра"||(type=="Поступление"&&J.S(o,"category").ToLowerInvariant().Contains("партн"));
   if(type=="Передача партнёру"){Stock(t,Home,item,-q);Stock(t,wh==Home?Partner:wh,item,q);}
   if(type=="Возврат от партнёра"){Stock(t,wh==Home?Partner:wh,item,-q);Stock(t,Home,item,q);}
   if(type=="Производство"||type=="Корректировка склада")Stock(t,wh,item,q);
   if(type=="Списание")Stock(t,wh,item,-q);
   if(type=="Покупка материалов")Stock(t,"Материалы",item,q);
   if(type=="Расход материалов")Stock(t,"Материалы",item,-q);
   if(type=="Продажа"){
    Stock(t,wh,item,-q);decimal paid=J.N(o,"paid",wh==Home?a-fee:0);
    Debt(t,who,a-fee-paid);t.CashFlow+=paid;
    if(period){t.Revenue+=a;t.Expense+=fee;t.Sales++;if(o.ContainsKey("_cost"))t.Cogs+=J.N(o,"_cost");else t.UnknownCost++;}
   }else if(partnerIncome){Debt(t,who,-a);t.CashFlow+=a;if(period)t.Payouts+=a;}
   else if(type=="Поступление"){t.CashFlow+=a;if(period)t.OtherIncome+=a;}
   else if(type=="Расход"){t.CashFlow-=a;if(period)t.Expense+=a;}
   else if(type=="Покупка материалов")t.CashFlow-=a;
   else if(type=="Вложение владельца")t.CashFlow+=a;
   else if(type=="Вывод владельцу")t.CashFlow-=a;
   else if(type=="Списание"&&period)t.Expense+=a;
   if(!allowed.Contains(type))t.Warnings.Add("Неизвестный тип: "+type+" (№"+J.S(o,"id")+")");
  }
  int undated=J.Rows(d,"operations").Count(o=>Active(o)&&J.S(o,"date")=="Без даты");if(undated>0)t.Warnings.Add(undated+" записей без даты: в остатках учтены как начальные, в отчёт за конкретный период не входят");
  t.Debt=t.Debts.Values.Sum();foreach(var kv in t.Stock.Where(k=>k.Value<0))t.Warnings.Add("Отрицательный остаток: "+kv.Key.Replace('\u001f','/')+" = "+kv.Value);
  foreach(var kv in t.Debts.Where(k=>k.Value<0))t.Warnings.Add("Переплата: "+kv.Key+" "+J.Money(-kv.Value));
  return t;
 }
 public static void ValidateOperation(Dictionary<string,object> db,Dictionary<string,object> o){
  string type=J.S(o,"type");if(!Types.Contains(type))throw new Exception("Выберите тип операции");J.Date(J.S(o,"date"));
  decimal a=J.N(o,"amount"),q=J.N(o,"qty"),fee=J.N(o,"fee"),paid=J.N(o,"paid");
  if(a<0||a!=Decimal.Round(a,2)||a>1000000000)throw new Exception("Сумма должна быть неотрицательной, до двух знаков после запятой");
  if(Math.Abs(q)>1000000000||q!=Decimal.Round(q,3))throw new Exception("Количество: до трёх знаков после запятой");
  bool inventory=new[]{"Продажа","Производство","Передача партнёру","Возврат от партнёра","Корректировка склада","Списание","Покупка материалов","Расход материалов"}.Contains(type);
  if(inventory&&(String.IsNullOrWhiteSpace(J.S(o,"item"))||q==0||(type!="Корректировка склада"&&q<0)))throw new Exception("Укажите изделие и положительное количество; корректировка допускает минус");
  if(type=="Продажа"&&(fee<0||paid<0||fee>a||paid>a-fee))throw new Exception("Комиссия и полученная оплата не должны превышать сумму продажи");
  if((type=="Продажа"||type=="Выплата партнёра")&&a==0)throw new Exception("Укажите сумму больше нуля");
  if(type=="Продажа"&&J.S(o,"warehouse")==Home&&paid<a-fee&&String.IsNullOrWhiteSpace(J.S(o,"counterparty")))throw new Exception("Укажите покупателя, который должен доплатить");
 }
 public static void ValidateChange(Dictionary<string,object> before,Dictionary<string,object> after){
  J.Validate(after);var b=Calculate(before);var a=Calculate(after);
  foreach(var p in a.Minimum){decimal prior;b.Minimum.TryGetValue(p.Key,out prior);if(p.Value<prior)throw new Exception("Операция создаёт нехватку на складе: "+p.Key.Replace('\u001f','/')+". Сначала внесите остаток или производство.");}
  foreach(var p in a.Stock){decimal prior;b.Stock.TryGetValue(p.Key,out prior);if(p.Value<Math.Min(0,prior))throw new Exception("Недостаточно остатка: "+p.Key.Replace('\u001f','/'));}
  foreach(var p in a.Debts){decimal prior;b.Debts.TryGetValue(p.Key,out prior);if(p.Value<Math.Min(0,prior))throw new Exception("Оплата превышает долг: "+p.Key);}
 }
}
public class Store:IDisposable {
 public string PathName;public Dictionary<string,object> Data;string fingerprint="";FileStream guard;
 public Store(string path){Open(path);}
 static string Hash(byte[] b){using(var h=SHA256.Create())return Convert.ToBase64String(h.ComputeHash(b));}
 public void Open(string path){
  path=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(path));
  FileStream next=null;try{
   next=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
   byte[] bytes=File.Exists(path)?File.ReadAllBytes(path):null;var data=bytes==null?J.Empty():J.Parse(Encoding.UTF8.GetString(bytes));J.Validate(data);
   if(guard!=null)guard.Dispose();guard=next;next=null;PathName=path;Data=data;fingerprint=bytes==null?"":Hash(bytes);
  }finally{if(next!=null)next.Dispose();}
 }
 public string Backup(){
  if(!File.Exists(PathName))return "";
  string dir=Path.Combine(Path.GetDirectoryName(PathName),"backups");Directory.CreateDirectory(dir);
  string p=Path.Combine(dir,"accounts_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fffffff")+".json");File.Copy(PathName,p);
  foreach(var old in new DirectoryInfo(dir).GetFiles("accounts_*.json").OrderByDescending(x=>x.Name).Skip(30))old.Delete();return p;
 }
 public void Save(Dictionary<string,object> next){
  J.Validate(next);if(File.Exists(PathName)&&Hash(File.ReadAllBytes(PathName))!=fingerprint)throw new Exception("Файл базы изменён другой программой. Откройте его заново перед сохранением.");
  string temp=PathName+".tmp-"+Guid.NewGuid().ToString("N");
  try{
   Backup();byte[] bytes=Encoding.UTF8.GetBytes(J.Dump(next));using(var f=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){f.Write(bytes,0,bytes.Length);f.Flush(true);}
   if(File.Exists(PathName))File.Replace(temp,PathName,null);else File.Move(temp,PathName);
   Data=next;fingerprint=Hash(bytes);
  }finally{if(File.Exists(temp))File.Delete(temp);}
 }
 public void ChangeOperation(Dictionary<string,object> operation,int editId=0){
  Engine.ValidateOperation(Data,operation);var next=J.Copy(Data);var ops=J.Rows(next,"operations");
  if(editId>0){int index=ops.FindIndex(x=>J.N(x,"id")==editId);if(index<0)throw new Exception("Запись не найдена");var old=ops[index];var history=J.Rows(next,"_audit");history.Add(new Dictionary<string,object>{{"at",DateTime.Now.ToString("o")},{"action","edit"},{"before",J.Copy(old)}});next["_audit"]=history;operation["id"]=editId;ops[index]=operation;}
  else{operation["id"]=ops.Count==0?1:ops.Max(x=>J.N(x,"id"))+1;ops.Add(operation);}
  next["operations"]=ops;Engine.ValidateChange(Data,next);Save(next);
 }
 public void Void(int id,string reason){if(String.IsNullOrWhiteSpace(reason))throw new Exception("Укажите причину отмены");var n=J.Copy(Data);var o=J.Rows(n,"operations").First(x=>J.N(x,"id")==id);o["_void"]=true;o["_voidReason"]=reason;o["_voidAt"]=DateTime.Now.ToString("o");Engine.ValidateChange(Data,n);Save(n);}
 public void ExportAndroid(string path){var n=J.Copy(Data);n["operations"]=J.Rows(n,"operations").Where(Engine.Active).ToList();n.Remove("_audit");File.WriteAllText(path,J.Dump(n),new UTF8Encoding(false));}
 public void Dispose(){if(guard!=null){guard.Dispose();guard=null;}}
}
}
