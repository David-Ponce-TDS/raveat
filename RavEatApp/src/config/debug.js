// Unico lugar que resuelve la URL base de la API: repetida en cada service, cambiar de maquina
// obligaria a buscarla en varios archivos.
// El predeterminado sirve para el navegador. En el telefono, localhost es el propio telefono:
// para el APK cada uno pone la IP de su maquina en VITE_API_URL_DEBUG.
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
