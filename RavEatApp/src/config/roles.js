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
