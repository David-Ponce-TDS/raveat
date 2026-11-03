import { SecureStorage } from '@aparajita/capacitor-secure-storage';
import { biometria_disponible, nombre_biometria, verificar_identidad } from '@/services/biometria_service';
import { restaurar_sesion } from '@/services/token_service';

// "Pedir Face ID al abrir": una preferencia del dispositivo, no de la cuenta. Vive en el
// almacenamiento seguro junto a la sesion que protege, y por eso se apaga sola con ella si el
// usuario borra los datos de la app.
const CLAVE_BLOQUEO = 'raveat_bloqueo_biometrico';

export async function bloqueo_activado(){
	try{
		return (await SecureStorage.get(CLAVE_BLOQUEO)) == '1';
	}catch{
		// El plugin tira si la clave no existe: sin preferencia guardada, el bloqueo esta apagado.
		return false;
	}
}

export async function guardar_bloqueo(activado){
	if(activado) await SecureStorage.set(CLAVE_BLOQUEO, '1');
	else await SecureStorage.remove(CLAVE_BLOQUEO);
}

// Encender exige confirmar con la biometria en ese momento: si no funciona, mejor enterarse
// ahora y no con la app cerrada y la sesion inaccesible.
export async function activar_bloqueo(){
	if(!(await biometria_disponible())) return {ok: false, mensaje: 'Este dispositivo no tiene biometría disponible.'};
	const confirmado = await verificar_identidad('Confirmá tu identidad para activar el bloqueo');
	if(!confirmado) return {ok: false, mensaje: null};
	await guardar_bloqueo(true);
	return {ok: true, mensaje: null};
}

// Se llama una vez al abrir la app, antes de restaurar la sesion. Devuelve true si se puede seguir:
// sin bloqueo, sin sesion guardada o sin biometria no hay nada que pedir. Si el usuario cancela o
// falla, devuelve false y la sesion NO se restaura: la app cae al login, donde el boton de
// biometria vuelve a ofrecer el desbloqueo.
export async function pasar_bloqueo_de_arranque(){
	if(!(await bloqueo_activado())) return true;
	if(!(await restaurar_sesion())) return true;
	if(!(await biometria_disponible())) return true;
	return verificar_identidad('Confirmá tu identidad para abrir RavEat');
}

// Como se llama la biometria del dispositivo, para los textos de la app: en un iPhone con Face ID
// dice "Face ID", no "huella". Los numeros son el BiometryType del plugin.
const nombres_biometria = {
	1: 'Touch ID',
	2: 'Face ID',
	3: 'huella',
	4: 'reconocimiento facial',
	5: 'iris'
};

export async function etiqueta_biometria(){
	const tipo = await nombre_biometria();
	return nombres_biometria[tipo] || 'biometría';
}

export function es_biometria_facial(etiqueta){
	return etiqueta == 'Face ID' || etiqueta == 'reconocimiento facial';
}
