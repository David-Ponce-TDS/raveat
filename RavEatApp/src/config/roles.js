export const roles = [
	{
		id: 'administrador',
		codigo: 'ADMIN',
		nombre: 'Administrador'
	},
	{
		id: 'vendedor',
		codigo: 'VENDEDOR',
		nombre: 'Vendedor'
	},
	{
		id: 'proceso',
		codigo: 'PROCESO',
		nombre: 'Proceso'
	},
	{
		id: 'caja',
		codigo: 'CAJA',
		nombre: 'Caja'
	},
	{
		id: 'delivery',
		codigo: 'DELIVERY',
		nombre: 'Delivery'
	}
];

export function obtener_rol(rol_id){
	return roles.find(rol => rol.id === rol_id) || null;
}

// La API habla en codigos (`ADMIN`) y la navegacion en ids (`administrador`). La traduccion vive
// aca, en el unico archivo que conoce las dos formas: asi `navegacion.js` se sigue leyendo en
// castellano y el contrato con el servidor no se filtra a toda la app.
export function obtener_rol_por_codigo(codigo){
	if(!codigo) return null;
	return roles.find(rol => rol.codigo === codigo) || null;
}
