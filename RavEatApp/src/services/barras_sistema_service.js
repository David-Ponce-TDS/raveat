import { SystemBars, SystemBarsStyle } from '@capacitor/core';
import { StatusBar } from '@capacitor/status-bar';

// El color del header en cada tema: la barra de estado se pinta igual, asi parecen una sola.
const FONDO_BARRA = {oscuro: '#1f1f1f', claro: '#ffffff'};

// Las barras del sistema siguen al tema de RavEat, que es propio (raveat_tema) y no al modo
// oscuro de Android: con la app en oscuro y el telefono en claro, la hora y la senal salian
// oscuras sobre un header oscuro. Este modulo es el unico que toca las barras.
//
// Dos pasos, porque Android cambio de politica a mitad de camino. Hasta Android 14 la barra de
// estado es una franja aparte, con su propio fondo: se pinta del color del header. Desde Android
// 15 la app se dibuja de borde a borde y la barra queda transparente sobre el header, asi que
// el fondo lo da el propio header y setBackgroundColor no hace nada. Lo que vale en los dos
// casos es el estilo: el COLOR DE LOS ICONOS, que Capacitor no puede adivinar.
export async function aplicar_estilo_barras(tema_oscuro){
	try{
		// El estilo describe el CONTENIDO de la barra, no el fondo: Dark pinta iconos claros.
		await SystemBars.setStyle({style: tema_oscuro ? SystemBarsStyle.Dark : SystemBarsStyle.Light});
		await StatusBar.setBackgroundColor({color: tema_oscuro ? FONDO_BARRA.oscuro : FONDO_BARRA.claro});
	}catch{
		// En el navegador no hay barras del sistema: no hay nada que hacer.
	}
}
