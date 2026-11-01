using System;
using System.Linq;
using System.Collections.Generic;
using Hryushki;
namespace Hryushki.Extensions {
public class ProfitabilityPlugin:IAccountsPlugin {
 public PluginReport Execute(string snapshot){var settings=Settings.Read("profit.settings.json",typeof(ProfitabilityPlugin));return Build(J.Parse(snapshot),DateTime.Today,Settings.Days(settings,"recentDays",30));}
 public static PluginReport Build(Dictionary<string,object> db,DateTime asOf,int days){
  if(days<1||days>3650)throw new Exception("Период должен быть от 1 до 3650 дней");J.Validate(db);asOf=asOf.Date;
  var sales=J.Rows(db,"operations").Where(o=>Engine.Active(o)&&J.S(o,"type")=="Продажа"&&J.Date(J.S(o,"date"))<=asOf).ToList();
  var report=new PluginReport{Title="На чём зарабатываем",Columns=new[]{"Период","Изделие / канал","Продано, шт.","Выручка","Комиссии","Себестоимость","Маржа","Полнота данных"},Rows=new List<string[]>()};
  int missing=sales.Count(o=>!o.ContainsKey("_cost"));int undated=sales.Count(o=>J.S(o,"date")=="Без даты");
  report.Summary="На "+asOf.ToString("dd.MM.yyyy")+". Маржа без общих расходов. Нет затрат: "+missing+" продаж; без даты: "+undated+". Срезы: все даты и "+days+" дней. Фильтр программы не применяется.";
  for(int scope=0;scope<2;scope++){
   var selected=scope==0?sales:sales.Where(o=>J.Date(J.S(o,"date"))>=asOf.AddDays(1-days)).ToList();
   foreach(var g in selected.GroupBy(o=>new {Name=J.S(o,"item","Без названия"),Channel=String.IsNullOrWhiteSpace(J.S(o,"category"))?"Без канала":J.S(o,"category"),Warehouse=J.S(o,"warehouse")}).OrderByDescending(g=>g.Sum(o=>J.N(o,"amount")))){
    decimal revenue=g.Sum(o=>J.N(o,"amount")),fee=g.Sum(o=>J.N(o,"fee")),cost=g.Where(o=>o.ContainsKey("_cost")).Sum(o=>J.N(o,"_cost"));int unknown=g.Count(o=>!o.ContainsKey("_cost"));int noDate=g.Count(o=>J.S(o,"date")=="Без даты");
    string channel=g.Key.Channel=="Без канала"?g.Key.Warehouse:g.Key.Channel+" · "+g.Key.Warehouse;
    report.Rows.Add(new[]{scope==0?"Все даты":days+" дней",g.Key.Name+" / "+channel,Settings.Quantity(g.Sum(o=>J.N(o,"qty"))),J.Money(revenue),J.Money(fee),J.Money(cost)+(unknown>0?" (часть)":""),unknown>0?"Не рассчитана":J.Money(revenue-fee-cost),unknown>0?"Нет затрат: "+unknown+" из "+g.Count()+(noDate>0?"; без даты: "+noDate:""):"Полные"+(noDate>0?"; без даты: "+noDate:"")});
   }
  }
  if(report.Rows.Count==0)report.Summary+=" Проведённых продаж до выбранной даты нет.";
  return report;
 }
}
}
