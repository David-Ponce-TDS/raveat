// Unico lugar de la app que resuelve la URL base de la API. Si estuviera repetida en cada service,
// cambiar de maquina obligaria a buscarla en varios archivos.
//
// El valor predeterminado permite empezar en el navegador sin configurar nada. Un telefono no
// puede usar localhost: ahi vive el propio telefono, no la PC. Para probar el APK, cada alumno
// configura en su .env la IP de su maquina mediante VITE_API_URL_DEBUG.
const api_url_debug_predeterminada = 'http://localhost:5080';

export const debug_config = {
	debug_activado: import.meta.env.VITE_DEBUG_ACTIVADO !== 'false',
	api_url_debug: (import.meta.env.VITE_API_URL_DEBUG || api_url_debug_predeterminada).replace(/\/+$/, '')
};

export function obtener_api_url(){
	const api_url_entorno = (import.meta.env.VITE_API_URL || '').replace(/\/+$/, '');
	if(debug_config.debug_activado) return debug_config.api_url_debug;
	return api_url_entorno;
}
