import { BarcodeFormat, BarcodeScanner } from '@capacitor-mlkit/barcode-scanning';
import { Capacitor } from '@capacitor/core';

// Escaneo con el lector de Google (ML Kit). Devuelve siempre {ok, contenido, mensaje}.
// El QR del comprobante trae el codigo del pedido (PED-0001): escanear devuelve ese texto.
export async function escanear_qr(){
	// El lector no existe en el navegador: ahi el camino es tipear el codigo en el buscador.
	if(!Capacitor.isNativePlatform()){
		return {
			ok: false,
			contenido: null,
			mensaje: 'El escáner solo funciona en la app instalada. Tipeá el código del pedido en el buscador.'
		};
	}
	try{
		const soporte = await BarcodeScanner.isSupported();
		if(!soporte.supported){
			return {
				ok: false,
				contenido: null,
				mensaje: 'Este dispositivo no puede escanear códigos. Tipeá el código del pedido en el buscador.'
			};
		}
		// La primera vez Android descarga el lector aparte: "todavia no esta" tambien es respuesta.
		const modulo = await BarcodeScanner.isGoogleBarcodeScannerModuleAvailable();
		if(!modulo.available){
			await BarcodeScanner.installGoogleBarcodeScannerModule();
			return {
				ok: false,
				contenido: null,
				mensaje: 'Se está descargando el lector de códigos de Google. Reintentá en unos segundos.'
			};
		}
		const resultado = await BarcodeScanner.scan({formats: [BarcodeFormat.QrCode]});
		const contenido = (resultado && resultado.barcodes && resultado.barcodes[0] && resultado.barcodes[0].rawValue) || '';
		if(!contenido){
			return {
				ok: false,
				contenido: null,
				mensaje: 'No se leyó ningún código.'
			};
		}
		return {ok: true, contenido, mensaje: null};
	}catch(error){
		// Cerrar el lector sin escanear no es un error: vuelve sin mensaje.
		if(/cancel/i.test(String((error && error.message) || ''))) return {ok: false, contenido: null, mensaje: null};
		return {
			ok: false,
			contenido: null,
			mensaje: (error && error.message) || 'No se pudo abrir el escáner.'
		};
	}
}
