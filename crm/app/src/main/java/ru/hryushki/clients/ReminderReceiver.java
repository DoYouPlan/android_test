package ru.hryushki.clients;
import android.content.*;
public class ReminderReceiver extends BroadcastReceiver {
 @Override public void onReceive(Context c,Intent i){String action=i.getAction();if(!java.util.Arrays.asList(Reminders.ACTION,Intent.ACTION_BOOT_COMPLETED,Intent.ACTION_MY_PACKAGE_REPLACED,Intent.ACTION_TIME_CHANGED,Intent.ACTION_TIMEZONE_CHANGED).contains(action))return;PendingResult result=goAsync();new Thread(()->{try{Reminders.refresh(c.getApplicationContext());}finally{result.finish();}},"order-reminders").start();}
}
