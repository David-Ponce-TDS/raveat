// La copia local: la ultima respuesta que dio la API a cada lectura marcada como `guardable`.
// Sin red, ajax_service devuelve esta copia y la pantalla muestra lo ultimo que se vio.
const prefijo = 'raveat_copia:';

export function guardar_copia(endpoint, respuesta){
	localStorage.setItem(prefijo + endpoint, JSON.stringify(respuesta));
}

export function obtener_copia(endpoint){
	const copia = localStorage.getItem(prefijo + endpoint);
	return copia ? JSON.parse(copia) : null;
}

// Al cerrar la sesion no queda nada: las copias son de quien las cargo.
export function borrar_copias(){
	Object.keys(localStorage)
		.filter(clave => clave.startsWith(prefijo))
		.forEach(clave => localStorage.removeItem(clave));
}
