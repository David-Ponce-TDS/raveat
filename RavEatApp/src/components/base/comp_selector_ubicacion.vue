<template>
	<ion-modal :is-open="abierto" @willPresent="preparar" @didPresent="medir" @didDismiss="$emit('cerrar')">
		<ion-header>
			<ion-toolbar>
				<ion-title>{{ titulo }}</ion-title>
				<ion-buttons slot="end">
					<ion-button @click="$emit('cerrar')">Cerrar</ion-button>
				</ion-buttons>
			</ion-toolbar>
		</ion-header>
		<ion-content :scroll-y="false">
			<div class="raveat-mapa">
				<!-- El respaldo, arriba de todo: la direccion escrita se busca y centra el mapa.
				     Sale al enviar, no por tecla: Nominatim es gratuito y pide no abusar. -->
				<div class="raveat-mapa__buscador">
					<form @submit.prevent="buscar">
						<ion-searchbar v-model="busqueda" placeholder="Buscar la dirección" :debounce="0" enterkeyhint="search" />
					</form>
					<ion-list v-if="resultados.length > 0" class="raveat-mapa__resultados" lines="full">
						<ion-item v-for="resultado in resultados" :key="resultado.nombre" button :detail="false" @click="elegir_resultado(resultado)">
							<ion-label class="ion-text-wrap">{{ resultado.nombre }}</ion-label>
						</ion-item>
					</ion-list>
				</div>

				<!-- Tiles de OpenStreetMap puestas a mano: sin libreria ni clave de API. El punto es
				     siempre el centro: se mueve el mapa debajo del pin, no el pin sobre el mapa. -->
				<div
					ref="superficie"
					class="raveat-mapa__superficie"
					@pointerdown="apoyar"
					@pointermove="mover"
					@pointerup="soltar"
					@pointercancel="soltar"
					@wheel.prevent="cambiar_zoom($event.deltaY < 0 ? 1 : -1)"
				>
					<img
						v-for="tile in tiles"
						:key="tile.id"
						class="raveat-mapa__tile"
						:src="tile.url"
						:style="{ left: `${tile.left}px`, top: `${tile.top}px` }"
						alt=""
						draggable="false"
						@load="tiles_cargadas++"
						@error="tiles_con_error++"
					/>
					<div class="raveat-mapa__zoom">
						<ion-button size="small" aria-label="Acercar" @click.stop="cambiar_zoom(1)">
							<ion-icon slot="icon-only" :icon="icono_mas" />
						</ion-button>
						<ion-button size="small" aria-label="Alejar" @click.stop="cambiar_zoom(-1)">
							<ion-icon slot="icon-only" :icon="icono_menos" />
						</ion-button>
					</div>
					<span class="raveat-mapa__atribucion">© OpenStreetMap</span>
					<div class="raveat-mapa__pin"></div>
				</div>

				<div class="raveat-mapa__pie">
					<!-- Sin tiles no hay mapa, pero hay salida: la direccion escrita ya alcanza para
					     que el repartidor la busque en su app de mapas. -->
					<comp-aviso v-if="mapa_caido" tono="pendiente" titulo="El mapa no cargó" mensaje="Cerrá y guardá con la dirección escrita: el mapa del repartidor la busca igual." />
					<comp-aviso v-if="mensaje" tono="error" :mensaje="mensaje" />
					<p class="raveat-item-meta">{{ movido ? coordenadas_texto : 'Mové el mapa hasta que el pin quede en la puerta.' }}</p>
					<div class="raveat-acciones">
						<ion-button fill="outline" :disabled="ubicando" @click="centrar_en_mi_ubicacion">
							<ion-spinner v-if="ubicando" slot="start" name="crescent" />
							<ion-icon v-else slot="start" :icon="icono_ubicacion" />
							Mi ubicación
						</ion-button>
						<ion-button :disabled="!movido || confirmando" @click="confirmar">
							<ion-spinner v-if="confirmando" slot="start" name="crescent" />
							Usar este punto
						</ion-button>
					</div>
				</div>
			</div>
		</ion-content>
	</ion-modal>
</template>

<script>import {
	IonButton,
	IonButtons,
	IonContent,
	IonHeader,
	IonIcon,
	IonItem,
	IonLabel,
	IonList,
	IonModal,
	IonSearchbar,
	IonSpinner,
	IonTitle,
	IonToolbar
} from '@ionic/vue';
import { addOutline, locateOutline, removeOutline } from 'ionicons/icons';
import comp_aviso from '@/components/base/comp_aviso.vue';
import { buscar_direccion_de_punto, buscar_direcciones, formatear_coordenadas, obtener_ubicacion_actual } from '@/services/geolocalizacion_service';

// Web Mercator, la proyeccion de todos los mapas de tiles: grados a pixeles del plano y vuelta.
// Es toda la matematica que hace falta para dibujar un mapa.
const TILE = 256;
const ZOOM_MINIMO = 3;
const ZOOM_MAXIMO = 19;
// Sin punto previo abre sobre la ciudad del local: mejor que el oceano en (0, 0).
const CENTRO_POR_DEFECTO = {latitud: -33.3017, longitud: -66.3378};

function a_pixel(latitud, longitud, zoom){
	const seno = Math.sin(latitud * Math.PI / 180);
	const escala = TILE * (2 ** zoom);
	return {
		x: (longitud + 180) / 360 * escala,
		y: (0.5 - Math.log((1 + seno) / (1 - seno)) / (4 * Math.PI)) * escala
	};
}

function a_grados(x, y, zoom){
	const escala = TILE * (2 ** zoom);
	const n = Math.PI - 2 * Math.PI * y / escala;
	return {
		latitud: 180 / Math.PI * Math.atan(Math.sinh(n)),
		longitud: x / escala * 360 - 180
	};
}

function distancia(a, b){
	return Math.hypot(a.x - b.x, a.y - b.y);
}

export default {
	name: 'comp_selector_ubicacion',
	components: {
		CompAviso: comp_aviso,
		IonButton,
		IonButtons,
		IonContent,
		IonHeader,
		IonIcon,
		IonItem,
		IonLabel,
		IonList,
		IonModal,
		IonSearchbar,
		IonSpinner,
		IonTitle,
		IonToolbar
	},
	emits: [
		'cerrar',
		'confirmar'
	],
	props: {
		abierto: {
			type: Boolean,
			default: false
		},
		titulo: {
			type: String,
			default: 'Marcar en el mapa'
		},
		// El punto que ya tenia el cliente, para abrir donde quedo.
		latitud: {
			type: Number,
			default: null
		},
		longitud: {
			type: Number,
			default: null
		},
		// La direccion escrita arranca en el buscador.
		direccion: {
			type: String,
			default: ''
		}
	},
	data(){
		return {
			arrastre: null,
			busqueda: '',
			centro: {...CENTRO_POR_DEFECTO},
			confirmando: false,
			direccion_elegida: '',
			icono_mas: addOutline,
			icono_menos: removeOutline,
			icono_ubicacion: locateOutline,
			medidas: {ancho: 360, alto: 360},
			mensaje: '',
			movido: false,
			pellizco: null,
			punteros: {},
			resultados: [],
			tiles_cargadas: 0,
			tiles_con_error: 0,
			ubicando: false,
			zoom: 16
		};
	},
	computed: {
		// Las tiles que cubren la superficie, mas una de margen por lado para que al arrastrar no
		// se vea el borde vacio.
		tiles(){
			var vm = this;
			const centro = a_pixel(vm.centro.latitud, vm.centro.longitud, vm.zoom);
			const ancho = vm.medidas.ancho;
			const alto = vm.medidas.alto;
			const por_lado = 2 ** vm.zoom;
			const tiles = [];
			for(let x = Math.floor((centro.x - ancho / 2) / TILE) - 1; x <= Math.floor((centro.x + ancho / 2) / TILE) + 1; x++){
				for(let y = Math.floor((centro.y - alto / 2) / TILE) - 1; y <= Math.floor((centro.y + alto / 2) / TILE) + 1; y++){
					if(y < 0 || y >= por_lado) continue;
					tiles.push({
						id: `${vm.zoom}-${x}-${y}`,
						url: `https://tile.openstreetmap.org/${vm.zoom}/${(x % por_lado + por_lado) % por_lado}/${y}.png`,
						left: Math.round(x * TILE - centro.x + ancho / 2),
						top: Math.round(y * TILE - centro.y + alto / 2)
					});
				}
			}
			return tiles;
		},
		// Ninguna tile llego y varias fallaron: sin red, o el servidor de tiles no responde.
		mapa_caido(){
			var vm = this;
			return vm.tiles_cargadas == 0 && vm.tiles_con_error >= 4;
		},
		coordenadas_texto(){
			var vm = this;
			return formatear_coordenadas(vm.centro.latitud, vm.centro.longitud);
		}
	},
	methods: {
		// Con punto previo abre ahi y ya vale; sin el, confirmar el centro por defecto seria
		// inventar un punto: hay que mover el mapa primero.
		preparar: function(){
			var vm = this;
			vm.mensaje = '';
			vm.resultados = [];
			vm.busqueda = vm.direccion;
			vm.zoom = 16;
			vm.tiles_cargadas = 0;
			vm.tiles_con_error = 0;
			vm.confirmando = false;
			vm.direccion_elegida = '';
			vm.movido = vm.latitud != null && vm.longitud != null;
			vm.centro = vm.movido ? {latitud: vm.latitud, longitud: vm.longitud} : {...CENTRO_POR_DEFECTO};
		},
		// La superficie mide 0 hasta que el modal termina de abrir: por eso se mide en didPresent.
		medir: function(){
			var vm = this;
			const caja = vm.$refs.superficie.getBoundingClientRect();
			vm.medidas = {ancho: caja.width, alto: caja.height};
		},
		// Un dedo arrastra; el segundo empieza un pellizco.
		apoyar: function(evento){
			var vm = this;
			evento.currentTarget.setPointerCapture(evento.pointerId);
			vm.punteros = {...vm.punteros, [evento.pointerId]: {x: evento.clientX, y: evento.clientY}};
			const puntos = Object.values(vm.punteros);
			if(puntos.length == 1){
				vm.arrastre = {x: evento.clientX, y: evento.clientY, centro: a_pixel(vm.centro.latitud, vm.centro.longitud, vm.zoom)};
				vm.pellizco = null;
			}else if(puntos.length == 2){
				vm.pellizco = {distancia: distancia(puntos[0], puntos[1]), zoom: vm.zoom};
				vm.arrastre = null;
			}
		},
		mover: function(evento){
			var vm = this;
			if(!vm.punteros[evento.pointerId]) return;
			vm.punteros = {...vm.punteros, [evento.pointerId]: {x: evento.clientX, y: evento.clientY}};
			const puntos = Object.values(vm.punteros);
			if(vm.pellizco && puntos.length == 2){
				// Cada vez que la distancia entre los dedos se duplica, un nivel mas de zoom.
				const razon = distancia(puntos[0], puntos[1]) / vm.pellizco.distancia;
				vm.zoom = Math.max(ZOOM_MINIMO, Math.min(ZOOM_MAXIMO, Math.round(vm.pellizco.zoom + Math.log2(razon))));
				return;
			}
			if(!vm.arrastre) return;
			// El mapa sigue al dedo: el centro se corre al reves de lo que se arrastra.
			vm.centro = a_grados(
				vm.arrastre.centro.x - (evento.clientX - vm.arrastre.x),
				vm.arrastre.centro.y - (evento.clientY - vm.arrastre.y),
				vm.zoom
			);
			vm.movido = true;
			vm.direccion_elegida = '';
		},
		soltar: function(evento){
			var vm = this;
			const punteros = {...vm.punteros};
			delete punteros[evento.pointerId];
			vm.punteros = punteros;
			vm.arrastre = null;
			vm.pellizco = null;
		},
		cambiar_zoom: function(paso){
			var vm = this;
			vm.zoom = Math.max(ZOOM_MINIMO, Math.min(ZOOM_MAXIMO, vm.zoom + paso));
		},
		buscar: async function(){
			var vm = this;
			vm.mensaje = '';
			const resultado = await buscar_direcciones(vm.busqueda);
			vm.resultados = resultado.resultados;
			if(!resultado.ok) vm.mensaje = resultado.mensaje;
			else if(resultado.resultados.length == 0) vm.mensaje = 'No se encontró esa dirección. Probá con calle, número y ciudad.';
		},
		// El resultado acerca, pero no decide: el pin se termina de poner a mano en la puerta.
		elegir_resultado: function(resultado){
			var vm = this;
			vm.centro = {latitud: resultado.latitud, longitud: resultado.longitud};
			vm.zoom = 18;
			vm.resultados = [];
			vm.movido = true;
			vm.direccion_elegida = resultado.direccion;
			vm.busqueda = resultado.direccion;
		},
		centrar_en_mi_ubicacion: async function(){
			var vm = this;
			vm.ubicando = true;
			vm.mensaje = '';
			const resultado = await obtener_ubicacion_actual();
			vm.ubicando = false;
			if(!resultado.ok){
				vm.mensaje = resultado.mensaje;
				return;
			}
			vm.centro = {latitud: resultado.ubicacion.latitud, longitud: resultado.ubicacion.longitud};
			vm.zoom = 18;
			vm.movido = true;
			vm.direccion_elegida = '';
		},
		// Siete decimales son un centimetro: mas no agrega nada. La direccion es la del resultado
		// elegido o, si el pin se movio, la que Nominatim da para el punto; sin red sale vacia y el
		// formulario se queda con la escrita.
		confirmar: async function(){
			var vm = this;
			const punto = {
				latitud: Number(vm.centro.latitud.toFixed(7)),
				longitud: Number(vm.centro.longitud.toFixed(7))
			};
			let direccion = vm.direccion_elegida;
			if(!direccion){
				vm.confirmando = true;
				const resultado = await buscar_direccion_de_punto(punto.latitud, punto.longitud);
				vm.confirmando = false;
				direccion = resultado.direccion;
			}
			vm.$emit('confirmar', {...punto, direccion});
		}
	}
};</script>
