<template>
	<!-- Sin menu: la pagina vive fuera de /app, donde no hay menu lateral que abrir. -->
	<comp-page titulo="Tu pedido" :con_menu="false" :mostrar_actualizar="true" @actualizar="cargar">
		<div class="ion-padding raveat-page-stack">
			<comp-esqueleto v-if="cargando" />

			<!-- El QR de un pedido cancelado ya no muestra la ficha: lo que ese papel prometía no
			     existe más, y reintentar no lo revive. Lo único útil que queda es la carta. -->
			<template v-else-if="cancelado">
				<div class="raveat-card raveat-publico">
					<p class="raveat-publico__codigo">Pedido cancelado</p>
					<p class="raveat-publico__meta">{{ error }}</p>
				</div>
				<ion-button expand="block" @click="seguir">
					<ion-icon slot="start" :icon="icono_carta" />
					{{ sesion_store.autenticado ? 'Ir al inicio' : 'Ver la carta' }}
				</ion-button>
			</template>

			<!-- Un enlace que no corresponde a ningun pedido no se arregla reintentando: la salida va
			     al lado, igual que en la pagina que sirve la API. -->
			<template v-else-if="error">
				<comp-estado-error :mensaje="error" @reintentar="cargar" />
				<ion-button expand="block" fill="outline" @click="seguir">
					<ion-icon slot="start" :icon="icono_carta" />
					{{ sesion_store.autenticado ? 'Ir al inicio' : 'Ver la carta' }}
				</ion-button>
			</template>

			<template v-else-if="pedido">
				<div class="raveat-card raveat-publico">
					<p class="raveat-publico__codigo">{{ pedido.codigo }}</p>
					<div class="raveat-publico__estados">
						<span class="raveat-pill" :class="clase_tinte(estado_pedido_publico(pedido.estado).color)">{{ estado_pedido_publico(pedido.estado).texto }}</span>
						<span class="raveat-pill" :class="clase_tinte(estado_pago(pedido.estado_pago).color)">{{ estado_pago(pedido.estado_pago).texto }}</span>
					</div>
					<p class="raveat-publico__meta">{{ tipo_pedido(pedido.tipo).texto }} · {{ formatear_fecha_larga(pedido.creado_en) }}</p>
				</div>

				<ion-list class="raveat-card">
					<ion-item v-for="(item, indice) in pedido.items" :key="indice" lines="inset">
						<ion-label>
							<h3>{{ item.nombre }}</h3>
							<p>{{ item.cantidad }} × {{ formatear_importe(item.subtotal / item.cantidad) }}</p>
						</ion-label>
						<ion-note slot="end">{{ formatear_importe(item.subtotal) }}</ion-note>
					</ion-item>
					<ion-item lines="none">
						<ion-label><strong>Total</strong></ion-label>
						<ion-note slot="end"><strong>{{ formatear_importe(pedido.total) }}</strong></ion-note>
					</ion-item>
				</ion-list>

				<!-- La puede estar mirando un comensal, que no tiene por qué tener cuenta, o alguien
				     del local que escaneó el papel. Al del local se le ofrece la ficha operable. -->
				<ion-button v-if="puede_operar" expand="block" @click="abrir_en_pedidos">
					<ion-icon slot="start" :icon="icono_abrir" />
					Abrirlo en Pedidos
				</ion-button>
				<ion-button expand="block" fill="outline" @click="seguir">
					<ion-icon slot="start" :icon="icono_carta" />
					{{ sesion_store.autenticado ? 'Ir al inicio' : 'Ver la carta' }}
				</ion-button>

				<comp-aviso tono="info" mensaje="Esta vista muestra el estado del pedido. Para cobrarlo o cambiarlo hace falta entrar con tu cuenta." />
			</template>
		</div>
	</comp-page>
</template>

<script>import {
	IonButton,
	IonIcon,
	IonItem,
	IonLabel,
	IonList,
	IonNote
} from '@ionic/vue';
import { openOutline, restaurantOutline } from 'ionicons/icons';
import comp_aviso from '@/components/base/comp_aviso.vue';
import comp_esqueleto from '@/components/base/comp_esqueleto.vue';
import comp_estado_error from '@/components/base/comp_estado_error.vue';
import comp_page from '@/components/estructura/comp_page.vue';
import { obtener_navegacion_rol } from '@/config/navegacion';
import { obtener_pedido_publico } from '@/services/pedidos_service';
import { use_sesion_store } from '@/stores/sesion_store';
import { clase_tinte, estado_pago, estado_pedido_publico, tipo_pedido } from '@/utils/estados';
import { formatear_fecha_larga } from '@/utils/formato_fecha';
import { formatear_importe } from '@/utils/formato_moneda';

// La cara publica del pedido dentro de la app: se llega por el QR, con sesion o sin ella. Vive
// fuera de /app, sin menu ni tabs: quien entra por un enlace no viene navegando.
//
// Muestra lo que devuelve el endpoint publico, que es menos de lo que sabe la API: ni el cliente ni
// quien lo cargo. Ese recorte lo hace el servidor; si lo hiciera esta pantalla, los datos igual
// habrian viajado.
export default {
	name: 'pedido_publico_page',
	components: {
		CompAviso: comp_aviso,
		CompEsqueleto: comp_esqueleto,
		CompEstadoError: comp_estado_error,
		CompPage: comp_page,
		IonButton,
		IonIcon,
		IonItem,
		IonLabel,
		IonList,
		IonNote
	},
	data(){
		return {
			cancelado: false,
			cargando: false,
			error: null,
			icono_abrir: openOutline,
			icono_carta: restaurantOutline,
			pedido: null,
			sesion_store: use_sesion_store()
		};
	},
	computed: {
		// Los mismos roles que entran a Pedidos, leidos de la navegacion: ofrecer una ruta que el
		// guard va a rechazar seria peor que no ofrecerla.
		puede_operar(){
			var vm = this;
			if(!vm.sesion_store.autenticado) return false;
			return obtener_navegacion_rol(vm.sesion_store.rol_activo).some(item => item.id == 'pedidos');
		}
	},
	watch: {
		// El mismo componente puede recibir otro token sin desmontarse: un segundo QR escaneado
		// estando ya en esta pantalla.
		'$route.params.token'(){
			var vm = this;
			if(vm.$route.params.token) vm.cargar();
		}
	},
	mounted(){
		var vm = this;
		vm.cargar();
	},
	methods: {
		cargar: async function(){
			var vm = this;
			vm.cargando = true;
			vm.error = null;
			vm.cancelado = false;
			try{
				vm.pedido = await obtener_pedido_publico(vm.$route.params.token);
			}catch(error){
				vm.pedido = null;
				// Cancelado no es una falla: es la respuesta. No lleva "reintentar": se ofrece la carta.
				vm.cancelado = error.codigo == 'pedido_cancelado';
				vm.error = error.mensaje || 'No se pudo cargar el pedido.';
			}finally{
				vm.cargando = false;
			}
		},
		// Por codigo y no por id: el endpoint publico no devuelve el id interno.
		abrir_en_pedidos: function(){
			var vm = this;
			vm.$router.push({path: '/app/pedidos', query: {buscar: vm.pedido.codigo}});
		},
		// Entrar por un enlace deja la app sin historial: este es el camino hacia el resto.
		seguir: function(){
			var vm = this;
			vm.$router.push(vm.sesion_store.autenticado ? '/app/inicio' : '/vitrina');
		},
		clase_tinte,
		estado_pago,
		estado_pedido_publico,
		formatear_fecha_larga,
		formatear_importe,
		tipo_pedido
	}
};</script>
