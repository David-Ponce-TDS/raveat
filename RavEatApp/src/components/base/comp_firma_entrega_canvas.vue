<template>
	<div class="raveat-firma">
		<!-- El canvas es el único lugar de la app donde se dibuja a mano. No hay librería: son
		     tres eventos de puntero y un trazo. El `touch-action: none` del CSS es lo que evita
		     que el gesto de firmar arrastre la pantalla en vez de dibujar. -->
		<canvas
			ref="lienzo"
			class="raveat-firma__lienzo"
			@pointerdown="empezar"
			@pointermove="mover"
			@pointerup="terminar"
			@pointerleave="terminar"
			@pointercancel="terminar"
		/>
		<div class="raveat-firma__acciones">
			<ion-button size="small" fill="clear" color="medium" :disabled="!hay_trazo" @click="limpiar">
				<ion-icon slot="start" :icon="icono_limpiar" />
				Borrar
			</ion-button>
			<slot name="acciones" />
		</div>
	</div>
</template>

<script>import { IonButton, IonIcon } from '@ionic/vue';
import { refreshOutline } from 'ionicons/icons';

// Firma de conformidad. No sabe de entregas ni de la API: dibuja, avisa si hay trazo y entrega la
// imagen cuando se la piden (`obtener_archivo`, que usa la pagina desde su ref).
export default {
	name: 'comp_firma_entrega_canvas',
	components: {
		IonButton,
		IonIcon
	},
	emits: ['cambiar'],
	props: {
		alto: {
			type: Number,
			default: 180
		}
	},
	data(){
		return {
			contexto: null,
			dibujando: false,
			hay_trazo: false,
			icono_limpiar: refreshOutline,
			observador: null
		};
	},
	mounted(){
		var vm = this;
		vm.preparar_lienzo();
		// El lienzo suele montarse adentro de un modal QUE TODAVIA SE ESTA ABRIENDO: en ese momento
		// clientWidth no es el ancho final, y un mapa de bits armado con el ancho equivocado dibuja
		// el trazo corrido respecto del dedo. El observer re-prepara cuando el tamaño real llega
		// (y de paso cubre el resize de la ventana).
		vm.observador = new ResizeObserver(function(){
			vm.preparar_lienzo();
		});
		vm.observador.observe(vm.$refs.lienzo);
	},
	beforeUnmount(){
		var vm = this;
		if(vm.observador) vm.observador.disconnect();
	},
	methods: {
		// El canvas tiene dos tamaños: el que ocupa en pantalla (CSS) y el de su mapa de bits. Si
		// no se ajusta al devicePixelRatio, en un teléfono la firma sale pixelada.
		preparar_lienzo: function(){
			var vm = this;
			const lienzo = vm.$refs.lienzo;
			if(!lienzo) return;
			const escala = window.devicePixelRatio || 1;
			const ancho = lienzo.clientWidth || 300;
			// Si el mapa de bits ya coincide, no hay nada que preparar: redimensionar borra el
			// trazo, y el observer avisa tambien por cambios que no cambian nada.
			if(lienzo.width == Math.round(ancho * escala) && lienzo.height == Math.round(vm.alto * escala)) return;
			lienzo.width = Math.round(ancho * escala);
			lienzo.height = Math.round(vm.alto * escala);
			lienzo.style.height = `${vm.alto}px`;
			const contexto = lienzo.getContext('2d');
			contexto.scale(escala, escala);
			contexto.lineWidth = 2;
			contexto.lineCap = 'round';
			contexto.lineJoin = 'round';
			contexto.strokeStyle = '#111111';
			vm.contexto = contexto;
			// Redimensionar borra el mapa de bits: el trazo anterior ya no está.
			vm.hay_trazo = false;
			vm.$emit('cambiar', false);
		},
		// El punto se escala del tamaño EN PANTALLA al del mapa de bits: si por un instante no
		// coinciden (el modal abriendose, una animacion), el trazo cae igual donde esta el dedo.
		punto: function(evento){
			var vm = this;
			const lienzo = vm.$refs.lienzo;
			const caja = lienzo.getBoundingClientRect();
			const escala = window.devicePixelRatio || 1;
			const factor_x = caja.width ? (lienzo.width / escala) / caja.width : 1;
			const factor_y = caja.height ? (lienzo.height / escala) / caja.height : 1;
			return {x: (evento.clientX - caja.left) * factor_x, y: (evento.clientY - caja.top) * factor_y};
		},
		empezar: function(evento){
			var vm = this;
			if(!vm.contexto) return;
			vm.dibujando = true;
			// Capturar el puntero mantiene el trazo aunque el dedo se salga del recuadro.
			vm.$refs.lienzo.setPointerCapture(evento.pointerId);
			const punto = vm.punto(evento);
			vm.contexto.beginPath();
			vm.contexto.moveTo(punto.x, punto.y);
		},
		mover: function(evento){
			var vm = this;
			if(!vm.dibujando) return;
			const punto = vm.punto(evento);
			vm.contexto.lineTo(punto.x, punto.y);
			vm.contexto.stroke();
			if(!vm.hay_trazo){
				vm.hay_trazo = true;
				vm.$emit('cambiar', true);
			}
		},
		terminar: function(){
			var vm = this;
			vm.dibujando = false;
		},
		limpiar: function(){
			var vm = this;
			const lienzo = vm.$refs.lienzo;
			if(!lienzo || !vm.contexto) return;
			vm.contexto.clearRect(0, 0, lienzo.width, lienzo.height);
			vm.hay_trazo = false;
			vm.$emit('cambiar', false);
		},
		// Devuelve la firma como archivo PNG listo para el FormData. La promesa es porque
		// `toBlob` es asincrónico: el navegador codifica la imagen fuera del hilo principal.
		obtener_archivo: function(){
			var vm = this;
			return new Promise(function(resolver){
				if(!vm.hay_trazo || !vm.$refs.lienzo) return resolver(null);
				vm.$refs.lienzo.toBlob(function(blob){
					resolver(blob ? new File([blob], 'firma.png', {type: 'image/png'}) : null);
				}, 'image/png');
			});
		}
	}
};</script>

