import { Capacitor } from '@capacitor/core';
import { Directory, Filesystem } from '@capacitor/filesystem';
import { Share } from '@capacitor/share';

// Compartir un archivo (el comprobante en PDF). Devuelve siempre {ok, mensaje}.
//   - Android: se escribe a disco y se comparte la URI (el sistema no lee un Blob de la webview)
//   - Navegador: navigator.share con un File y, si no sabe compartir archivos, la descarga
export async function compartir_archivo({nombre_archivo, blob, titulo, texto}){
	if(!blob) return {ok: false, mensaje: 'No hay archivo para compartir.'};
	try{
		if(!Capacitor.isNativePlatform()) return await compartir_archivo_web({nombre_archivo, blob, titulo, texto});
		const base64 = await blob_a_base64(blob);
		const escrito = await Filesystem.writeFile({
			path: nombre_archivo,
			data: base64,
			// Cache: la copia ya viajo y el sistema la puede limpiar sin perder nada.
			directory: Directory.Cache
		});
		await Share.share({
			title: titulo,
			text: texto,
			files: [escrito.uri],
			dialogTitle: titulo || 'Compartir comprobante'
		});
		return {ok: true, mensaje: null};
	}catch(error){
		return {ok: false, mensaje: normalizar_mensaje(error)};
	}
}

async function compartir_archivo_web({nombre_archivo, blob, titulo, texto}){
	const archivo = new File([blob], nombre_archivo, {type: blob.type || 'application/pdf'});
	if(navigator.canShare && navigator.canShare({files: [archivo]})){
		await navigator.share({title: titulo, text: texto, files: [archivo]});
		return {ok: true, mensaje: null};
	}
	descargar_en_navegador(nombre_archivo, blob);
	return {ok: true, mensaje: 'El navegador no permite compartir archivos: se descargó el comprobante.'};
}

function descargar_en_navegador(nombre_archivo, blob){
	const url = window.URL.createObjectURL(blob);
	const enlace = document.createElement('a');
	enlace.href = url;
	enlace.download = nombre_archivo;
	document.body.appendChild(enlace);
	enlace.click();
	document.body.removeChild(enlace);
	window.URL.revokeObjectURL(url);
}

function blob_a_base64(blob){
	return new Promise((resolve, reject) =>{
		const lector = new FileReader();
		lector.onerror = () => reject(new Error('No se pudo leer el archivo.'));
		lector.onload = () =>{
			const resultado = String(lector.result || '');
			resolve(resultado.slice(resultado.indexOf(',') + 1));
		};
		lector.readAsDataURL(blob);
	});
}

function normalizar_mensaje(error){
	const mensaje = (error && error.message) || '';
	// Cancelar el dialogo no es una falla: vuelve sin mensaje.
	if(/cancel/i.test(mensaje)) return null;
	return mensaje || 'No se pudo compartir.';
}
