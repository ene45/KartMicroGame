# Arcos integrados con la pared

Prefab: **Assets/MedievalCartoon/Prefabs/Arquitectura/Portal_Integrado_Pared_21_6m.prefab**.

Es un portal de piedra gris cartoon con pared alrededor del arco: los laterales y las esquinas superiores quedan cerrados. El relleno se prolonga hasta el dintel y se introduce un poco en los muros contiguos para evitar rendijas. Tiene grosor, se ve desde ambos lados y conserva un paso de 21,6 m en la base para la pista de 18 m.

Para encontrarlo: **Tools → Dungeon Track → Portales → Mostrar arco integrado en Project**. También podés buscar Portal_Integrado en Project y arrastrar el prefab a la escena.

Para colocarlo en los dos accesos del circuito: **Tools → Dungeon Track → Portales → Integrar entrada y salida en la escena actual**. La acción admite Ctrl+Z y no crea duplicados si se ejecuta otra vez. Los arcos originales quedan desactivados para poder recuperarlos; no se vuelve a generar la pista ni se cambia el resto de la pared.

En **DungeonCircuit_AnilloLava 1**, los objetos se llaman **Entrada_Portal_Integrado** y **Salida_Portal_Integrado**, dentro de **CIRCUITO_MAZMORRA_PROBUILDER → 05_Arcos_Entrada_Salida**.

Para editar con ProBuilder, expandí el portal y seleccioná una de estas piezas:

- **01_Relleno_Pared_Laterales_Y_Sobre_Arco**: la pared que tapa los huecos.
- **02_Arco_Dovelas_Editable**: la curva del arco y sus bloques de piedra.
- **03_Pilares_Editable**: los dos pilares.
- **04_Clave_Piedra_Editable**: la piedra central superior.

Todas tienen ProBuilderMesh y collider. El pivote del prefab está en el centro del paso, a la altura del piso. La fachada mide 25,8 m de ancho y llega a 16,3 m por encima del piso; el muro de la sala existente continúa hasta el techo. Antes de modificar una instancia de forma independiente, usá **Prefab → Unpack** y conservá una copia como respaldo.
