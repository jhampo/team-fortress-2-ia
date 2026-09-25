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

## Subir a GitHub

Desde esta carpeta, usando Git o GitHub Desktop, crea el primer commit y publica en `https://github.com/jhampo/team-fortress-2-ia.git`. La copia ya incluye `.gitignore` y `.gitattributes`. No se ha realizado ninguna subida ni se han guardado credenciales.

La carpeta ocupa aproximadamente 1,23 GB y contiene cerca de 9900 archivos. Utiliza Git o GitHub Desktop, no la carga manual de archivos desde el navegador. El archivo individual mas grande ronda los 39,5 MB. En esta copia no se han configurado filtros Git LFS.

Si usas Git:

```sh
git init
git add .
git commit -m "Initial Unity project"
git branch -M main
git remote add origin https://github.com/jhampo/team-fortress-2-ia.git
git push -u origin main
```

## Recursos y derechos

Proyecto de aficionados inspirado en Team Fortress 2. Los recursos de terceros conservan los derechos de sus autores; esta copia no concede una licencia para redistribuirlos. Revisa los permisos antes de publicar el repositorio. Se mantienen los creditos y la procedencia de audio en `Assets/ControlPoints/AUDIO_SOURCES.json` y `AUDIO_README.md`.
