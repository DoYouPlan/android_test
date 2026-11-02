package ru.hryushki.clients;
import android.app.*;
import android.content.*;
import android.graphics.Bitmap;
import android.os.SystemClock;
import android.view.*;
import android.widget.*;
import androidx.test.platform.app.InstrumentationRegistry;
import androidx.test.ext.junit.runners.AndroidJUnit4;
import org.junit.Test;
import org.junit.runner.RunWith;
import org.json.*;
import java.io.*;
import static org.junit.Assert.*;
@RunWith(AndroidJUnit4.class)
public class UiTest {
 static EditText field(MainActivity a,String label){for(int j=0;j<a.body.getChildCount()-1;j++){View v=a.body.getChildAt(j);if(v instanceof TextView&&((TextView)v).getText().toString().equals(label)&&a.body.getChildAt(j+1)instanceof EditText)return(EditText)a.body.getChildAt(j+1);}throw new AssertionError("Нет поля "+label);}
 static void click(MainActivity a,String title){for(int j=0;j<a.body.getChildCount();j++){View v=a.body.getChildAt(j);if(v instanceof Button&&((Button)v).getText().toString().equals(title)){v.performClick();return;}}throw new AssertionError("Нет кнопки "+title);}
 @Test public void formsAndScreens()throws Exception{Instrumentation i=InstrumentationRegistry.getInstrumentation();Context c=i.getTargetContext();Store store=new Store(c);JSONObject d=CrmTest.sample();JSONObject o=CrmTest.order(d);Model.upsert(d,"orders",o);JSONObject w=new JSONObject("{\"id\":\"wish-a\",\"clientId\":\"client-a\",\"product\":\"Домик\",\"color\":\"Синий со звёздами\",\"qty\":1,\"status\":\"Ожидает\",\"kind\":\"Пожелание\"}");Model.upsert(d,"wishes",w);store.replace(d);Intent launch=new Intent(c,MainActivity.class).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);MainActivity a=(MainActivity)i.startActivitySync(launch);i.waitForIdleSync();
  i.runOnMainSync(()->{try{a.editClient(Model.find(a.store.data,"clients","client-a"));field(a,"Город").setText("Омск");click(a,"Сохранить");assertEquals("",a.lastError);assertEquals("Омск",Model.s(Model.find(a.store.data,"clients","client-a"),"city"));JSONObject order=Model.list(a.store.data,"orders").get(0);a.editOrder(order);field(a,"Трек-номер").setText("TEST-001");click(a,"Сохранить");assertEquals("",a.lastError);assertEquals("TEST-001",Model.s(Model.find(a.store.data,"orders",Model.s(order,"id")),"tracking"));a.editWish(Model.find(a.store.data,"wishes","wish-a"),"client-a");field(a,"Цвет / ткань").setText("Синий флис");click(a,"Сохранить");assertEquals("",a.lastError);assertEquals("Синий флис",Model.s(Model.find(a.store.data,"wishes","wish-a"),"color"));}catch(Exception e){throw new RuntimeException(e);}});i.waitForIdleSync();SystemClock.sleep(3500);
  String[] pages={"Сегодня","Клиенты","Заказы","Пожелания","База и настройки","Клиент"};for(int n=0;n<pages.length;n++){String page=pages[n];i.runOnMainSync(()->{a.clientId="client-a";a.navigate(page);});i.waitForIdleSync();SystemClock.sleep(350);assertEquals("",a.lastError);Bitmap screenshot=i.getUiAutomation().takeScreenshot();assertNotNull(screenshot);try(FileOutputStream out=new FileOutputStream(new File(c.getFilesDir(),"crm-screen-"+n+".png"))){screenshot.compress(Bitmap.CompressFormat.PNG,100,out);}screenshot.recycle();}i.runOnMainSync(a::finish);
 }
}
