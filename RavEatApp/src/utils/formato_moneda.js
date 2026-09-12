// Formateo de importes en pesos argentinos, compartido por toda la app.
const formateador_ars = new Intl.NumberFormat('es-AR', {
	style: 'currency',
	currency: 'ARS',
	maximumFractionDigits: 0
});

export function formatear_importe(valor){
	return formateador_ars.format(valor || 0);
}
