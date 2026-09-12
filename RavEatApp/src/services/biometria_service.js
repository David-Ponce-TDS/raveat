import { BiometricAuth } from '@aparajita/capacitor-biometric-auth';

// La biometria NO reemplaza al login: no hay forma de que una huella se convierta en un token.
// Lo que hace es **desbloquear la sesion que ya esta guardada** en el almacenamiento seguro.
// El usuario entra una vez con email y contrasena; a partir de ahi, la huella evita volver a
// escribirlos. Si nunca hubo un login, no hay nada que desbloquear.

export async function biometria_disponible(){
	try{
		const resultado = await BiometricAuth.checkBiometry();
		return Boolean(resultado?.isAvailable);
	}catch{
		// En el navegador el plugin no existe: no es un error, es que no hay biometria.
		return false;
	}
}

export async function nombre_biometria(){
	try{
		const resultado = await BiometricAuth.checkBiometry();
		return resultado?.biometryType ? String(resultado.biometryType) : '';
	}catch{
		return '';
	}
}

// Devuelve true solo si el sistema confirmó la identidad. Cualquier otra cosa —cancelado, sin
// huellas cargadas, demasiados intentos— es false: el que decide es el sistema operativo, y la app
// no interpreta el motivo para no terminar tratando un error como un exito.
export async function verificar_identidad(motivo = 'Confirmá tu identidad para entrar'){
	try{
		await BiometricAuth.authenticate({
			reason: motivo,
			cancelTitle: 'Cancelar',
			allowDeviceCredential: true,
			androidTitle: 'RavEat',
			androidSubtitle: motivo,
			androidConfirmationRequired: false
		});
		return true;
	}catch{
		return false;
	}
}
