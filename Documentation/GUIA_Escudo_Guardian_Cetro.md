# Escudo del guardián con cetro

Escudo decorativo 3D en relieve, con el busto gris de los guardianes del paquete medieval cartoon. Ambas manos sujetan una vara vertical delante del cuerpo; su remate ornamental queda abajo, con una flor de lis y una gema azul pequeña, siguiendo la referencia proporcionada.

## Encontrarlo

1. En Unity, ejecutá **Assets → Refresh** y esperá a que termine la compilación.
2. Abrí **Tools → Assets de Mazmorra → Abrir biblioteca**.
3. Pulsá **Mostrar escudo del guardián con cetro**. También podés ir a **Assets/MedievalCartoon/Prefabs/Decoracion/Escudo_Guardian_Cetro.prefab**.
4. Arrastrá el prefab a Scene o Hierarchy. Seleccionalo y pulsá **F** en Scene para enfocarlo.

El menú **Tools → Assets de Mazmorra → Escudo del guardián → Agregar a escena** también lo añade. La acción se puede deshacer con Ctrl+Z.

## Ajustarlo

La cara del escudo apunta hacia su eje local **+Z**. Su origen está en la parte inferior; la pieza mide aproximadamente **6,5 m de ancho y 6,7 m de alto** a escala 1. Cambiá el valor de **Scale** del objeto principal para reducirlo o usarlo como emblema monumental en una pared.

Expandí el prefab para modificar sus cuatro grupos:

- **01_Escudo_Fondo_Y_Marco**: contorno exterior y panel de piedra oscura.
- **02_Guardian_En_Relieve**: busto, casco y armadura del guardián, con poca profundidad.
- **03_Brazos_Y_Dos_Manos**: brazos, palmas y dedos que sujetan la vara.
- **04_Cetro_Hacia_Abajo**: vara, flor de lis inferior y gema azul.

Podés mover las manos y el cetro como objetos hijos, y cambiar sus materiales desde Inspector. Si movés la vara lateralmente, ajustá también las manos para mantener el contacto. Las mallas son normales de Unity; las piezas se ajustan con Transform. No hay animación, daño ni colisiones en este asset decorativo.

## Archivos entregados

- **Escudo_Guardian_Cetro.unitypackage**: el prefab y todas sus mallas, materiales y textura de piedra necesarios. Requiere URP. No reemplaza los otros guardianes ni las escenas del circuito.
- **Escudo_Guardian_Cetro_Frontal.png**: captura frontal del modelo real en Unity.
- **Escudo_Guardian_Cetro_Relieve.png**: captura oblicua que muestra el volumen del relieve.

El asset reutiliza el guardián y la textura gris del paquete anterior. Se modeló y se capturó en Unity; no se generó una imagen nueva con IA para sustituir el modelo 3D.
