package ru.hryushki.accounts;
import android.app.*;
import android.content.*;
import android.graphics.Bitmap;
import android.widget.*;
import android.view.View;
import androidx.test.platform.app.InstrumentationRegistry;
import androidx.test.ext.junit.runners.AndroidJUnit4;
import org.junit.Test;
import org.junit.runner.RunWith;
import org.json.*;
import java.io.*;
import static org.junit.Assert.*;
@RunWith(AndroidJUnit4.class)
public class UiCapture {
 @Test public void editAndCapture()throws Exception{
  Instrumentation i=InstrumentationRegistry.getInstrumentation();Context ctx=i.getTargetContext();DatabaseStore store=new DatabaseStore(ctx);JSONObject db=Engine.empty();JSONObject product=new JSONObject("{\"name\":\"Лежанка · зелёный флис\",\"sku\":\"LZ-001\",\"model\":\"Лежанка\",\"fabric\":\"Зелёный флис\",\"HomePrice\":680,\"PartnerPrice\":650,\"Cost\":300}");Engine.rows(db,"products").put(product);AccountingTest.append(db,AccountingTest.op(1,"Производство",Engine.HOME,product.getString("name"),"5","0"));AccountingTest.append(db,AccountingTest.op(2,"Передача партнёру",Engine.PARTNER,product.getString("name"),"2","0"));JSONObject sale=AccountingTest.op(3,"Продажа",Engine.PARTNER,product.getString("name"),"1","650");sale.put("_cost",300);AccountingTest.append(db,sale);store.save(db);
  Intent intent=new Intent(ctx,MainActivity.class);intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);MainActivity a=(MainActivity)i.startActivitySync(intent);i.waitForIdleSync();
  i.runOnMainSync(()->{try{a.editOperation(Engine.rows(a.store.data,"operations").getJSONObject(2),null);edit(a,"Комментарий / номер заказа").setText("Проверено редактирование");click(a,"Сохранить операцию");assertEquals("",a.lastError);assertEquals("Проверено редактирование",Engine.rows(a.store.data,"operations").getJSONObject(2).getString("note"));assertEquals(1,Engine.rows(a.store.data,"_audit").length());a.editCatalog(false,Engine.rows(a.store.data,"products").getJSONObject(0));edit(a,"Артикул, например LZ-001").setText("LZ-010");click(a,"Сохранить карточку");assertEquals("",a.lastError);assertEquals("LZ-010",Engine.rows(a.store.data,"products").getJSONObject(0).getString("sku"));}catch(Exception e){throw new RuntimeException(e);}});i.waitForIdleSync();
  String[] pages={"Обзор","Операции","Склад изделий","Товары и цены","Материалы","База и копии"};for(int index=0;index<pages.length;index++){String p=pages[index];i.runOnMainSync(()->a.navigate(p));i.waitForIdleSync();assertEquals("",a.lastError);Bitmap image=i.getUiAutomation().takeScreenshot();assertNotNull(image);try(FileOutputStream out=new FileOutputStream(new File(ctx.getFilesDir(),"screen-"+index+".png"))){image.compress(Bitmap.CompressFormat.PNG,100,out);}image.recycle();}
  i.runOnMainSync(a::finish);
 }
 static EditText edit(MainActivity a,String label){for(int j=0;j<a.body.getChildCount()-1;j++){View v=a.body.getChildAt(j);if(v instanceof TextView&&((TextView)v).getText().toString().equals(label)){View next=a.body.getChildAt(j+1);if(next instanceof EditText)return(EditText)next;}}throw new AssertionError("Missing field "+label);}
 static void click(MainActivity a,String title){for(int j=0;j<a.body.getChildCount();j++){View v=a.body.getChildAt(j);if(v instanceof Button&&((Button)v).getText().toString().equals(title)){v.performClick();return;}}throw new AssertionError("Missing button "+title);}
}
