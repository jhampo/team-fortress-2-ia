# team-fortress-2-ia

Proyecto Unity de Powerhouse: juego de combate por equipos RED/BLU con Scout, Soldier, Pyro y Heavy, bots y tres puntos de control.

## Abrir el proyecto

1. Instala **Unity 6000.6.1f1** mediante Unity Hub.
2. En Unity Hub, selecciona **Add project from disk** y elige esta carpeta (la que contiene Assets, Packages y ProjectSettings).
3. Espera la restauracion de paquetes y la importacion inicial de recursos. Unity regenerara Library; no se incluye en Git.
4. Abre `Assets/Powerhouse/Scenes/Powerhouse.unity` y pulsa Play.

Para exportar a Windows o WebGL, instala el modulo correspondiente desde Unity Hub y utiliza Build Profiles. Los scripts de exportacion personalizados conservan rutas del equipo de desarrollo: adaptalas antes de usarlos en otro equipo.

## Contenido

- `Assets`: escenas, modelos importados, materiales, texturas, audio, animaciones y scripts del juego, con sus archivos `.meta`.
- `Packages`: dependencias y versiones de paquetes de Unity.
- `ProjectSettings`: configuracion del proyecto.

Esta copia no incluye ejecutables, Library, logs, configuraciones personales, videos, proyectos Blender, scripts externos de conversion ni respaldos de escenas. No requiere Blender para abrir los modelos importados. El complemento MCP de desarrollo no se incluye como dependencia.

## Controles

WASD: movimiento; Espacio: salto; Ctrl: agacharse; clic izquierdo: disparar; R: recargar; clic derecho: giro del Heavy o aire comprimido del Pyro; coma: elegir clase; TAB: marcador; ESC: opciones/menu.
