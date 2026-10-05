// Fechas y horas para las filas y fichas, en un solo lugar. 24 horas explicitas: el WebView de
// Android arma el formato de 12 sin el "a. m." y la hora se lee corrida.
const formateador_hora_corta = new Intl.DateTimeFormat('es-AR', {
	day: '2-digit',
	month: '2-digit',
	hour: '2-digit',
	minute: '2-digit',
	hour12: false
});

const formateador_fecha_larga = new Intl.DateTimeFormat('es-AR', {
	day: '2-digit',
	month: '2-digit',
	year: 'numeric',
	hour: '2-digit',
	minute: '2-digit',
	hour12: false
});

// "30/09 21:15": para la fila, donde el ano sobra.
export function formatear_hora_corta(valor){
	if(!valor) return '';
	return formateador_hora_corta.format(new Date(valor));
}

// "30/09/2026 21:15": para la ficha.
export function formatear_fecha_larga(valor){
	if(!valor) return '';
	return formateador_fecha_larga.format(new Date(valor));
}
