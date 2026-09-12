// Datos de prueba de clientes, con los nombres de campo en snake_case que va a devolver la API:
// cuando en v4 la lista venga del servidor, la pantalla no cambia.
// Las direcciones son reales de CABA porque en v7 se dibujan en un mapa: inventadas, los
// marcadores caen en el oceano.

export const CLIENTES = [
	{
		id: 1,
		nombre: 'María López',
		telefono: '11-5555-1001',
		email: 'maria.lopez@example.com',
		direccion_linea: 'Av. Corrientes 1368, CABA',
		direccion_referencia: 'Timbre 3B',
		direccion_latitud: -34.60396,
		direccion_longitud: -58.38603
	},
	{
		id: 2,
		nombre: 'Julián Pérez',
		telefono: '11-5555-1002',
		email: 'julian.perez@example.com',
		direccion_linea: 'Av. de Mayo 825, CABA',
		direccion_referencia: 'Edificio esquina, portón negro',
		direccion_latitud: -34.60874,
		direccion_longitud: -58.37892
	},
	{
		id: 3,
		nombre: 'Carla Fernández',
		telefono: '11-5555-1003',
		email: 'carla.fernandez@example.com',
		direccion_linea: 'Defensa 1179, San Telmo, CABA',
		direccion_referencia: 'Local a la calle',
		direccion_latitud: -34.62077,
		direccion_longitud: -58.37145
	},
	// Sin email ni referencia: si todos los datos de prueba estuvieran completos, el primer
	// cliente real con un campo vacio romperia la vista.
	{
		id: 4,
		nombre: 'Roberto Díaz',
		telefono: '11-5555-1004',
		email: null,
		direccion_linea: 'Av. Santa Fe 3253, Palermo, CABA',
		direccion_referencia: null,
		direccion_latitud: -34.58859,
		direccion_longitud: -58.41108
	},
	// Sin direccion: solo puede pedir para retirar, no para entrega.
	{
		id: 5,
		nombre: 'Lucía Giménez',
		telefono: '11-5555-1005',
		email: 'lucia.gimenez@example.com',
		direccion_linea: null,
		direccion_referencia: null,
		direccion_latitud: null,
		direccion_longitud: null
	}
];

const RETARDO_SIMULADO = 600;

export function obtener_clientes(){
	return new Promise(resolver =>{
		window.setTimeout(() => resolver({clientes: CLIENTES}), RETARDO_SIMULADO);
	});
}
