package ru.hryushki.clients;
import androidx.test.platform.app.InstrumentationRegistry;
import androidx.test.ext.junit.runners.AndroidJUnit4;
import org.junit.Test;
import org.junit.runner.RunWith;
import org.json.JSONObject;
import static org.junit.Assert.*;
@RunWith(AndroidJUnit4.class)
public class PersistenceCheck {
 @Test public void seed()throws Exception{Store s=new Store(InstrumentationRegistry.getInstrumentation().getTargetContext());s.change(d->d.put("restartProof","crm-persists"));}
 @Test public void verify()throws Exception{Store s=new Store(InstrumentationRegistry.getInstrumentation().getTargetContext());assertEquals("crm-persists",s.data.getString("restartProof"));assertTrue(s.data.getJSONArray("orders").length()>0);}
}
