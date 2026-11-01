using Hryushki;
using System;
using System.Linq;
using System.Collections.Generic;
public class SalesByWarehouse:IAccountsPlugin {
 public PluginReport Execute(string snapshotJson){
  var db=J.Parse(snapshotJson);var report=new PluginReport{Title="Продажи по складам",Columns=new[]{"Склад","Продаж","Выручка"},Rows=new List<string[]>(),Summary="Пример расширения API v1. Отменённые записи исключены. Все даты."};
  foreach(var group in J.Rows(db,"operations").Where(o=>Engine.Active(o)&&J.S(o,"type")=="Продажа").GroupBy(o=>J.S(o,"warehouse")))report.Rows.Add(new[]{group.Key,group.Count().ToString(),J.Money(group.Sum(o=>J.N(o,"amount")))});
  return report;
 }
}
