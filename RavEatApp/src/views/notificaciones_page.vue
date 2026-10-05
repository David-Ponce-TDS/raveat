<template>
	<comp-page titulo="Notificaciones" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<ion-button
				v-if="store.no_leidas > 0"
				expand="block"
				fill="outline"
				size="small"
				:disabled="store.cargando"
				@click="marcar_todas"
			>
				<ion-icon slot="start" :icon="icono_leidas" />
				Marcar todas como leídas ({{ store.no_leidas }})
			</ion-button>

			<comp-esqueleto v-if="store.cargando && !store.hay_notificaciones" />
			<comp-estado-error
				v-else-if="store.error && !store.hay_notificaciones"
				:mensaje="store.error"
				@reintentar="cargar"
			/>
			<comp-estado-vacio
				v-else-if="!store.hay_notificaciones"
				:icono="icono"
				titulo="Sin notificaciones"
				mensaje="Cuando pase algo que te interese, va a aparecer acá."
			/>
			<comp-lista
				v-else
				:cargar_mas="store.cargar_mas"
				:hay_mas="store.hay_mas"
				:mostrados="store.notificaciones.length"
				:total="store.resumen ? store.resumen.total : 0"
			>
				<!-- Tocar una no leida la marca; si nombra un pedido, ademas lo abre. El icono tenido
				     dice de que es; la leida se apaga, no se borra: sigue estando y se puede releer. -->
				<ion-item
					v-for="notificacion in store.notificaciones"
					:key="notificacion.id"
					:button="!notificacion.leida_en || Boolean(destino_pedido(notificacion))"
					lines="none"
					:detail="false"
					@click="tocar(notificacion)"
				>
					<div class="raveat-fila" :class="{ 'raveat-notificacion--leida': notificacion.leida_en }">
						<div class="raveat-icono-estado" :class="clase_tinte(notificacion.leida_en ? 'medium' : tipo_notificacion(notificacion.tipo).color)">
							<ion-icon :icon="tipo_notificacion(notificacion.tipo).icono" aria-hidden="true" />
						</div>
						<div class="raveat-fila__texto">
							<h3 class="raveat-item-title">{{ notificacion.titulo }}</h3>
							<p>{{ notificacion.mensaje }}</p>
							<p class="raveat-item-meta">
								{{ formatear_hora_corta(notificacion.creado_en) }}
								<template v-if="notificacion.leida_en"> · Leída {{ formatear_hora_corta(notificacion.leida_en) }}</template>
							</p>
						</div>
						<span v-if="!notificacion.leida_en" class="raveat-pill raveat-tinte-danger">Nueva</span>
					</div>
				</ion-item>
			</comp-lista>
		</div>
	</comp-page>
</template>

<script>import {
	IonButton,
	IonIcon,
	IonItem
} from '@ionic/vue';
import { checkmarkDoneOutline, notificationsOffOutline } from 'ionicons/icons';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_estado_vacio from '@/components/base/comp_estado_vacio.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_lista from '@/components/base/comp_lista.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import { avisar_exito } from '@/services/avisos_service';
import { clase_tinte, tipo_notificacion } from '@/utils/estados';
import { obtener_navegacion_rol } from '@/config/navegacion';
import { formatear_hora_corta } from '@/utils/formato_fecha';
import { use_notificaciones_store } from '@/stores/notificaciones_store';
import { use_sesion_store } from '@/stores/sesion_store';

export default {
	name: 'notificaciones_page',
	components: {
		CompEstadoError: comp_estado_error,
		CompEstadoVacio: comp_estado_vacio,
		CompEsqueleto: comp_esqueleto,
		CompLista: comp_lista,
		CompPage: comp_page,
		IonButton,
		IonIcon,
		IonItem
	},
	data(){
		return {
			icono: notificationsOffOutline,
			icono_leidas: checkmarkDoneOutline,
			sesion_store: use_sesion_store(),
			store: use_notificaciones_store()
		};
	},
	computed: {
		// El destino lo decide el rol con la configuracion de navegacion.js: un delivery o cocina
		// recibe avisos de pedidos pero no entra a Pedidos, y mandarlo ahi seria un rebote.
		puede_ver_pedidos(){
			var vm = this;
			return obtener_navegacion_rol(vm.sesion_store.rol_activo).some(item => item.id == 'pedidos');
		}
	},
	mounted(){
		var vm = this;
		vm.cargar();
	},
	methods: {
		cargar: function(){
			var vm = this;
			return vm.store.cargar();
		},
		// El aviso confirma lo que el contador ya muestra: sin el, tocar el boton y ver desaparecer
		// el numero se parece demasiado a que la pantalla se rompio.
		marcar_todas: async function(){
			var vm = this;
			const cuantas = vm.store.no_leidas;
			if(await vm.store.marcar_leida()) avisar_exito(cuantas == 1 ? 'Aviso marcado como leído.' : `${cuantas} avisos marcados como leídos.`);
		},
		// El codigo sale del titulo ("Pedido PED-0042 listo"): la notificacion no trae el id del
		// pedido, y el codigo es lo que el buscador de Pedidos entiende.
		destino_pedido: function(notificacion){
			var vm = this;
			if(!vm.puede_ver_pedidos) return '';
			const encontrado = String(notificacion.titulo || '').match(/PED-[0-9]+/);
			return encontrado ? encontrado[0] : '';
		},
		tocar: function(notificacion){
			var vm = this;
			if(!notificacion.leida_en) vm.store.marcar_leida(notificacion.id);
			const codigo = vm.destino_pedido(notificacion);
			if(codigo) vm.$router.push({path: '/app/pedidos', query: {buscar: codigo}});
		},
		clase_tinte,
		formatear_hora_corta,
		tipo_notificacion
	}
};</script>
