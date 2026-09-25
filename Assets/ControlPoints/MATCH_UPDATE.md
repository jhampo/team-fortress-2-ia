# Powerhouse — partida, interfaz y sonido

Cambios del 23 de septiembre de 2026.

## Uso

- Al iniciar Play aparece el menú principal con Scout. **JUGAR** inicia una preparación de 10 segundos.
- Las puertas de ambas bases permanecen cerradas y los bots en idle durante la preparación. El reloj de captura permanece en 5:00.
- **Tab**, mantenido: marcador RED/BLU con nombres, bajas, muertes, asistencias, capturas y puntos. El jugador se llama **player-astra-gpt**.
- **Coma (,)**: selector de Scout, Soldier, Pyro y Heavy. Cambiar dentro de la base propia es inmediato; fuera provoca muerte y espera de reaparición. Durante la espera se puede modificar la clase pendiente sin reiniciar la cuenta.
- La dificultad se elige en el **menú principal**, antes de JUGAR.
- **ESC**: pausa con una única opción, **IR AL MENÚ**; ESC otra vez continúa. Volver al menú termina y reinicia la partida.
- Durante los 10 segundos se puede mover, saltar, disparar, recargar y cambiar de clase. Los bots siguen en idle y el temporizador de puntos no avanza.
- El botón de sonido del menú silencia/restaura todo el audio y conserva la preferencia.

## Reglas y presentación

- Puntuación: 2 por baja, 1 por asistencia y 3 por captura. Asistencias: compañeros que dañaron al enemigo durante los 8 segundos previos. Suicidio/cambio de clase: una muerte, sin otorgar baja.
- Puntos RED/BLU alineados con el centro real de las escotillas: X = ±92; Z = -4,84. Efectos y área de captura comparten posición.
- Cuatro barreras continuas para malla ciclónica y 42 colisiones de barandillas/rejas. Se conserva la apertura central y se reconstruyó la navegación.
- Explosión del Soldier: fogonazo, núcleo, fuego irregular, humo retardado, chispas, onda breve, iluminación y sonido. Daño y velocidad del misil sin cambios.
- Los sonidos sintetizados se sustituyeron por 96 archivos descargados de TF2/Source: armas, giro, fuego, recarga, pasos por superficie, impactos, voces de daño/muerte por clase, suministros, reaparición, capturas, cuenta atrás, puertas, interfaz y ambiente. Banco local precargado, sin red durante la partida. Ver `AUDIO_SOURCES.json` para procedencia y hashes; son recursos de Valve, no CC0 ni libres de derechos.
- Minigun y lanzallamas usan una sola fuente continua por personaje, con arranque/parada. Quemaduras en bucle separado; recargas sincronizadas con inserción de cartuchos/cohetes. Voces sin cambio aleatorio de tono y avisos de victoria/derrota según equipo.

## Rendimiento

- Pools de efectos de explosión y de fuentes de sonido; clips precargados.
- Geometría de fragmentación y poses relajadas preparadas antes de la partida.
- Rutas reutilizadas y percepción espaciada; actualización de suministros e interfaz a frecuencias limitadas.
- Se eliminaron asignaciones repetidas en la lógica de captura y en los desvíos de navegación.
- Muestra de 20 segundos de combate en Unity Editor, sin llamadas de herramientas durante la medición: mediana 11,31 ms; percentil 95 de 19,30 ms; máximo 60,47 ms. Es una medición local, no una garantía de ausencia total de tirones en todos los equipos.

## Verificación

Las comprobaciones cubren preparación/puertas, cambio de clase, bajas/asistencias, silenciamiento, fragmentación, persistencia de scripts y rutas. Se verificaron además las reglas y estadísticas existentes, suministros y animaciones de personajes. Los bots siguen usando rutas principales y alternativas.

Pruebas reutilizables: `CPMatchUpgradeTests.Geometry()` en edición y `CPMatchUpgradeTests.Runtime()` en una partida de prueba después de la preparación. Las pruebas runtime alteran esa partida; detener Play la descarta.

Copia previa de scripts y escena: `Assets/ControlPoints/Backups/BeforeMatchMenus/`. Los respaldos usan extensión `.txt` para no compilar copias duplicadas.
