package ru.hryushki.accounts;
import androidx.test.platform.app.InstrumentationRegistry;
import androidx.test.ext.junit.runners.AndroidJUnit4;
import org.junit.Test;
import org.junit.runner.RunWith;
import org.json.JSONObject;
import static org.junit.Assert.*;
@RunWith(AndroidJUnit4.class)
public class PersistenceCheck {
 @Test public void seed()throws Exception{DatabaseStore s=new DatabaseStore(InstrumentationRegistry.getInstrumentation().getTargetContext());JSONObject d=Engine.empty();d.put("restartProof","survives-force-stop");s.save(d);}
 @Test public void verify()throws Exception{DatabaseStore s=new DatabaseStore(InstrumentationRegistry.getInstrumentation().getTargetContext());assertEquals("survives-force-stop",s.data.getString("restartProof"));}
}
