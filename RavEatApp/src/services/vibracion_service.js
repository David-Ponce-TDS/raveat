import { Haptics, ImpactStyle, NotificationType } from '@capacitor/haptics';
import { use_app_store } from '@/stores/app_store';

// La capacidad nativa mas barata: VIBRATE es un permiso normal, Android lo concede al instalar
// y no hay dialogo que el usuario pueda rechazar.

// Si el usuario la apago en Mi cuenta, ningun rincon de la app vibra.
function vibracion_apagada(){
	return !use_app_store().vibracion_activa;
}

// Toque corto: respuesta tactil a una accion del usuario.
export async function vibrar_toque(){
	if(vibracion_apagada()) return;
	try{
		await Haptics.impact({style: ImpactStyle.Light});
	}catch{
		// Sin motor de vibracion (navegador de escritorio): silencio.
	}
}

// Patron de error: lo dispara ajax_service, asi que vale para toda pantalla.
export async function vibrar_error(){
	if(vibracion_apagada()) return;
	try{
		await Haptics.notification({type: NotificationType.Error});
	}catch{
		// Sin motor de vibracion: silencio.
	}
}
