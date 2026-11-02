package ru.hryushki.clients;
import android.app.*;
import android.content.*;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.os.Build;
import java.time.*;
import java.util.*;
import org.json.*;
public final class Reminders {
 public static final String CHANNEL="orders-v1",ACTION="ru.hryushki.clients.CHECK";
 public static void channel(Context c){NotificationChannel ch=new NotificationChannel(CHANNEL,"Заказы и пожелания",NotificationManager.IMPORTANCE_DEFAULT);ch.setDescription("Срок отправки, ожидаемая доставка и пожелания клиентов");c.getSystemService(NotificationManager.class).createNotificationChannel(ch);}
 public static boolean enabled(Context c){channel(c);return (Build.VERSION.SDK_INT<33||c.checkSelfPermission(android.Manifest.permission.POST_NOTIFICATIONS)==PackageManager.PERMISSION_GRANTED)&&c.getSystemService(NotificationManager.class).areNotificationsEnabled()&&c.getSystemService(NotificationManager.class).getNotificationChannel(CHANNEL).getImportance()!=NotificationManager.IMPORTANCE_NONE;}
 public static PendingIntent alarm(Context c){return PendingIntent.getBroadcast(c,1,new Intent(c,ReminderReceiver.class).setAction(ACTION),PendingIntent.FLAG_UPDATE_CURRENT|PendingIntent.FLAG_IMMUTABLE);}
 public static void refresh(Context c){refresh(c,System.currentTimeMillis());}
 public static void refresh(Context c,long now){synchronized(Store.LOCK){try{Store store=new Store(c);channel(c);android.content.SharedPreferences prefs=c.getSharedPreferences("reminders",0);Set<String> fired=new HashSet<>(prefs.getStringSet("fired",new HashSet<>()));List<Model.Event> events=Model.events(store.data,now,ZoneId.systemDefault());NotificationManager nm=c.getSystemService(NotificationManager.class);Set<String> valid=new HashSet<>();for(Model.Event e:events)valid.add(e.key);if(Build.VERSION.SDK_INT>=23)for(android.service.notification.StatusBarNotification n:nm.getActiveNotifications())if(n.getTag()!=null&&!valid.contains(n.getTag()))nm.cancel(n.getTag(),0);
  long next=LocalDate.now().plusDays(1).atTime(store.data.getJSONObject("settings").optInt("reminderHour",9),0).atZone(ZoneId.systemDefault()).toInstant().toEpochMilli();
  for(Model.Event e:events){if(!fired.contains(e.key)){if(e.at<=now&&enabled(c)){post(c,e);fired.add(e.key);}else if(e.at>now)next=Math.min(next,e.at);}}
  if(fired.size()>5000){Set<String> keep=new HashSet<>(valid);fired.retainAll(keep);}prefs.edit().putStringSet("fired",fired).remove("error").apply();c.getSystemService(AlarmManager.class).setAndAllowWhileIdle(AlarmManager.RTC_WAKEUP,Math.max(next,now+60000),alarm(c));
 }catch(Exception e){c.getSharedPreferences("reminders",0).edit().putString("error",e.getMessage()).apply();}}}
 public static void post(Context c,Model.Event e){Intent open=new Intent(c,MainActivity.class).setData(Uri.parse("hryushki://order/"+e.orderId)).putExtra("orderId",e.orderId).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK|Intent.FLAG_ACTIVITY_CLEAR_TOP|Intent.FLAG_ACTIVITY_SINGLE_TOP);PendingIntent tap=PendingIntent.getActivity(c,0,open,PendingIntent.FLAG_UPDATE_CURRENT|PendingIntent.FLAG_IMMUTABLE);Notification publicNotice=new Notification.Builder(c,CHANNEL).setSmallIcon(R.drawable.ic_notice).setContentTitle("Хрюшки · Заказы").setContentText("Есть напоминание по заказу или пожеланию").build();Notification notification=new Notification.Builder(c,CHANNEL).setSmallIcon(R.drawable.ic_notice).setContentTitle(e.title).setContentText(e.text).setStyle(new Notification.BigTextStyle().bigText(e.text)).setContentIntent(tap).setAutoCancel(true).setVisibility(Notification.VISIBILITY_PRIVATE).setPublicVersion(publicNotice).build();c.getSystemService(NotificationManager.class).notify(e.key,0,notification);}
 public static void test(Context c){channel(c);if(!enabled(c))throw new IllegalArgumentException("Разрешите уведомления в настройках телефона");post(c,new Model.Event("test","","Напоминания работают","Здесь будут сроки отправки и доставки",System.currentTimeMillis()));}
}
