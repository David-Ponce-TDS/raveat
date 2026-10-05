// Como se PINTA una categoria: su acento y su icono. Igual que utils/estados.js, toda pantalla
// lee de aca y ninguna inventa los suyos.
import {
	beerOutline,
	fastFoodOutline,
	iceCreamOutline,
	leafOutline,
	pizzaOutline,
	restaurantOutline
} from 'ionicons/icons';

// El acento sale del id, no de la base: la misma categoria se pinta igual en toda la app sin
// guardar un color en ningun lado, y una categoria nueva recibe uno solo.
const PALETA_CATEGORIA = ['success', 'warning', 'danger', 'primary', 'tertiary'];

// El icono sale del nombre, con un cubierto generico de respaldo: las categorias las crea el
// usuario y una sin palabra conocida no puede quedarse sin icono.
const ICONOS_CATEGORIA = [
	{clave: 'hamburgues', icono: fastFoodOutline},
	{clave: 'entrada', icono: leafOutline},
	{clave: 'pizza', icono: pizzaOutline},
	{clave: 'bebida', icono: beerOutline},
	{clave: 'postre', icono: iceCreamOutline}
];

export function acento_categoria(categoria_id){
	const id = Number(categoria_id) || 0;
	const largo = PALETA_CATEGORIA.length;
	return PALETA_CATEGORIA[((id - 1) % largo + largo) % largo];
}

export function icono_categoria(nombre){
	const texto = String(nombre || '').toLowerCase();
	const encontrado = ICONOS_CATEGORIA.find(item => texto.includes(item.clave));
	return encontrado ? encontrado.icono : restaurantOutline;
}
