package app.raveat;

import com.getcapacitor.BridgeActivity;

public class MainActivity extends BridgeActivity {
    @Override
    public void onCreate(android.os.Bundle savedInstanceState) {
        // Los plugins propios se registran ANTES de super.onCreate: ahi se crea el bridge, y uno
        // registrado despues no existe para el JavaScript.
        registerPlugin(GoogleAuthPlugin.class);
        super.onCreate(savedInstanceState);
    }
}
