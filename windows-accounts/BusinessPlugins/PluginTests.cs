using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Hryushki;
using Hryushki.Extensions;
public static class PluginTests {
 static int count;
 static void Check(bool v,string name){if(!v)throw new Exception(name);count++;Console.WriteLine("OK "+name);}
 static Dictionary<string,object> Op(int id,string type,string item,decimal q,decimal a,string wh="Дома"){return new Dictionary<string,object>{{"id",id},{"date","02.11.2026"},{"type",type},{"item",item},{"qty",q},{"amount",a},{"warehouse",wh}};}
 public static int Main(string[] args){try{
  DateTime today=new DateTime(2026,11,2);var db=J.Empty();var sale=Op(2,"Продажа","Домик",1,1000);sale["fee"]=100;sale["_cost"]=300;
  var cancelled=Op(3,"Продажа","Домик",9,9000);cancelled["_void"]=true;
  var future=Op(4,"Продажа","Домик",1,1000);future["date"]="03.11.2026";
  db["operations"]=new[]{sale,cancelled,future};var profit=ProfitabilityPlugin.Build(db,today,30);PluginManager.ValidateReport(profit);
  Check(profit.Rows.Count==2,"periods exclude cancelled and future sales");Check(profit.Rows[0][6]==J.Money(600),"margin subtracts commission and historical cost");
  sale.Remove("_cost");db["products"]=new[]{new Dictionary<string,object>{{"name","Домик"},{"Cost",100}}};profit=ProfitabilityPlugin.Build(db,today,30);Check(profit.Rows[0][6]=="Не рассчитана","current cost never fills historical cost");
  sale["_cost"]=0;profit=ProfitabilityPlugin.Build(db,today,30);Check(profit.Rows[0][6]==J.Money(900),"explicit zero historical cost supported");
  sale["date"]="Без даты";profit=ProfitabilityPlugin.Build(db,today,30);Check(profit.Rows.Count==1,"undated sales only in all-history view");
  sale["date"]="04.10.2026";profit=ProfitabilityPlugin.Build(db,today,30);Check(profit.Rows.Count==2,"30 day window includes first day");sale["date"]="03.10.2026";profit=ProfitabilityPlugin.Build(db,today,30);Check(profit.Rows.Count==1,"30 day window excludes prior day");
  db=J.Empty();db["products"]=new[]{new Dictionary<string,object>{{"name","Тоннель"},{"HomePrice",500},{"PartnerPrice",500}},new Dictionary<string,object>{{"name","Неактивный"},{"HomePrice",0},{"PartnerPrice",0}}};
  db["operations"]=new[]{Op(1,"Корректировка склада","Тоннель",10,0)};
  var plan=StockPlannerPlugin.Build(db,today,60,21,2,1);PluginManager.ValidateReport(plan);var row=plan.Rows.First(x=>x[0]=="Тоннель");Check(row[5]=="1"&&row[6]=="0","transfer uses home surplus before production");Check(plan.Rows.First(x=>x[0]=="Неактивный")[6]=="0","inactive catalog is not manufactured");
  db["operations"]=new[]{Op(1,"Корректировка склада","Тоннель",0,0)};plan=StockPlannerPlugin.Build(db,today,60,21,2,1);row=plan.Rows.First(x=>x[0]=="Тоннель");Check(row[6]=="3"&&row[5]=="0","production covers both warehouse targets once");
  db["operations"]=new[]{Op(1,"Корректировка склада","Тоннель",-1,0)};plan=StockPlannerPlugin.Build(db,today,60,21,2,1);row=plan.Rows.First(x=>x[0]=="Тоннель");Check(row[6]=="—","negative stock blocks recommendation");
  var unknown=Op(2,"Продажа","Тоннель",500,0,Engine.Partner);unknown["date"]="Без даты";db["operations"]=new[]{Op(1,"Корректировка склада","Тоннель",500,0,Engine.Partner),unknown};plan=StockPlannerPlugin.Build(db,today,60,21,2,1);row=plan.Rows.First(x=>x[0]=="Тоннель");Check(row[3]=="0"&&row[6]=="3","undated quantity affects stock but not sales velocity");
  db["operations"]=new[]{Op(1,"Корректировка склада","Тоннель",100,0),Op(2,"Продажа","Тоннель",60,30000)};plan=StockPlannerPlugin.Build(db,today,60,21,2,1);row=plan.Rows.First(x=>x[0]=="Тоннель");Check(row[4]=="21 / 1"&&row[5]=="1"&&row[6]=="0","velocity target retains home reserve");
  string before=J.Dump(db);ProfitabilityPlugin.Build(db,today,30);StockPlannerPlugin.Build(db,today,60,21,2,1);Check(J.Dump(db)==before,"plugins never mutate snapshot");
  if(args.Length==3&&args[0]=="--lifecycle"){foreach(string package in args.Skip(1)){string temp=Path.Combine(Path.GetTempPath(),"BusinessPluginTest_"+Guid.NewGuid().ToString("N"));try{var manager=new PluginManager(temp);manager.Install(package);var info=manager.List().Single();Check(!info.Enabled,"package installed disabled");manager.Toggle(info);var report=manager.Run(info,J.Dump(J.Empty())).GetAwaiter().GetResult();PluginManager.ValidateReport(report);Check(report.Title=="На чём зарабатываем"||report.Title=="Что сшить и передать партнёру","packaged plugin runs in host");manager.Toggle(info);Check(!manager.List().Single().Enabled,"package disabled");manager.Remove(info);Check(manager.List().Count==0,"package uninstalled");}finally{if(Directory.Exists(temp))Directory.Delete(temp,true);}}}
  if(args.Length==1){var old=J.Parse(File.ReadAllText(args[0]));PluginManager.ValidateReport(ProfitabilityPlugin.Build(old,today,30));PluginManager.ValidateReport(StockPlannerPlugin.Build(old,today,60,21,2,1));Check(true,"existing version 2.1 fixture is supported");}
  Console.WriteLine("PASS "+count+" plugin checks");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
