<template>
	<ion-modal :is-open="abierto" @willPresent="desplazamiento = 0" @didDismiss="$emit('cerrar')">
		<!-- Sin hoja, la ficha de siempre: cabecera con titulo, acciones y Cerrar. -->
		<ion-header v-if="!con_hoja">
			<ion-toolbar>
				<ion-title>{{ titulo }}</ion-title>
				<ion-buttons slot="end">
					<slot name="acciones" />
					<ion-button @click="$emit('cerrar')">Cerrar</ion-button>
				</ion-buttons>
			</ion-toolbar>
		</ion-header>
		<ion-content :class="{ 'raveat-hoja-content': con_hoja }" :scroll-events="con_hoja" @ionScroll="al_scrollear">
			<!-- Cerrar va en el slot fixed de ion-content: flota sobre la foto y el scroll de la
			     hoja no se lo lleva. -->
			<ion-button v-if="con_hoja" slot="fixed" class="raveat-hoja__cerrar" aria-label="Cerrar" @click="$emit('cerrar')">
				<ion-icon slot="icon-only" :icon="icono_cerrar" />
			</ion-button>

			<!-- El mismo gesto que en las listas: una ficha abierta tambien envejece —otro usuario
			     cobra o mueve el pedido mientras esta en pantalla— y cerrar y volver a abrir para
			     ver lo nuevo es pedirle al usuario que adivine que la pantalla no se actualiza. -->
			<ion-refresher v-if="mostrar_actualizar" slot="fixed" @ionRefresh="refrescar">
				<ion-refresher-content pulling-text="Desliza para actualizar" refreshing-text="Actualizando..." />
			</ion-refresher>

			<template v-if="con_hoja">
				<!-- El fondo queda fijo (sticky) y la hoja desliza por encima. Con imagen es la
				     foto; sin imagen, un degradado del color del estado con lo que la ficha ponga
				     en el slot "hero". -->
				<!-- A medida que la hoja sube, el fondo se apaga: si no, cortaba el titulo y el
				     subtitulo por la mitad justo cuando pasaba por encima. -->
				<section class="raveat-hoja__fondo" :class="clase_fondo" :style="{opacity: opacidad_fondo}">
					<img v-if="imagen" :src="imagen" :alt="titulo" />
					<slot v-else name="hero" />
				</section>

				<div class="raveat-hoja">
					<div class="raveat-hoja__asa" aria-hidden="true"></div>
					<div class="ion-padding raveat-detalle">
						<slot />
					</div>
				</div>
			</template>
			<div v-else class="ion-padding raveat-detalle">
				<slot />
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
	IonModal,
	IonRefresher,
	IonRefresherContent,
	IonTitle,
	IonToolbar
} from '@ionic/vue';
import { closeOutline } from 'ionicons/icons';

// El armazon de cualquier ficha de la app. Tiene dos caras: la clasica, con cabecera y Cerrar, y
// la hoja deslizante, que se elige con `imagen` (la foto de fondo) o con `hero_color` (un fondo
// tenido con el resumen encima). Todas las fichas se abren y se cierran igual, y el estilo de la
// hoja vive en componentes.css, no en cada una.
export default {
	name: 'comp_modal_detalle',
	components: {
		IonButton,
		IonButtons,
		IonContent,
		IonHeader,
		IonIcon,
		IonModal,
		IonRefresher,
		IonRefresherContent,
		IonTitle,
		IonToolbar
	},
	emits: [
		'actualizar',
		'cerrar'
	],
	props: {
		abierto: {
			type: Boolean,
			default: false
		},
		// El color de la paleta para el fondo tenido, cuando no hay foto.
		hero_color: {
			type: String,
			default: ''
		},
		imagen: {
			type: String,
			default: ''
		},
		mostrar_actualizar: {
			type: Boolean,
			default: false
		},
		titulo: {
			type: String,
			default: ''
		}
	},
	data(){
		return {
			desplazamiento: 0,
			icono_cerrar: closeOutline
		};
	},
	computed: {
		con_hoja(){
			var vm = this;
			return Boolean(vm.imagen) || Boolean(vm.hero_color);
		},
		// De 1 a 0 en los primeros 110 px de scroll: cuando la hoja llega a tapar el texto, el
		// fondo ya se fue del todo.
		opacidad_fondo(){
			var vm = this;
			return Math.max(0, 1 - vm.desplazamiento / 110);
		},
		clase_fondo(){
			var vm = this;
			if(vm.imagen) return '';
			return `raveat-hoja__fondo--tinte raveat-hoja__fondo--${vm.hero_color}`;
		}
	},
	methods: {
		al_scrollear: function(evento){
			var vm = this;
			vm.desplazamiento = evento.detail.scrollTop;
		},
		refrescar: function(evento){
			var vm = this;
			vm.$emit('actualizar');
			// El spinner se cierra a mano, igual que en comp-page: sin datos que esperar, Ionic lo
			// dejaria girando para siempre.
			window.setTimeout(() => evento.detail.complete(), 350);
		}
	}
};</script>
