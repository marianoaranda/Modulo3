# PRD-001: Módulo de Stock — Genera pedidos de mercadería automáticamente.

## Contexto y Problema
En un pequeño comercio de barrio que compra y vende artículos (por ejemplo, un negocio de repuestos de artículos del hogar), resulta difícil
saber si se tiene en existencia la cantidad necesaria de cada artículo.

## Objetivos
Este módulo va a ser un SITIO WEB que, con solo registrar las ventas y las compras de mercadería, permita inferir automáticamente
la lista de artículos que hace falta pedir según los siguientes criterios:

- Pedir todos los artículos hasta alcanzar el stock mínimo de cada uno.
- Pedir todos los artículos hasta alcanzar el punto de pedido de cada uno.
- Pedir todos los artículos hasta alcanzar el stock ideal de cada uno.
- Pedir solo los artículos que estén por debajo del mínimo, hasta el stock mínimo de cada uno.
- Pedir solo los artículos que estén por debajo del mínimo, hasta el punto de pedido de cada uno.
- Pedir solo los artículos que estén por debajo del mínimo, hasta el stock ideal de cada uno.

## Requerimientos Funcionales

- RF-01: Debe tener una carga de perfiles (alta, baja y modificación) con los campos ID (autonumérico) y Descripción, donde se cargan los perfiles
         de seguridad (ejemplo: administrador, administrativo, vendedor).

- RF-02: Debe tener una carga de usuarios (alta, baja y modificación) con los siguientes campos: UsuarioId, Usuario, NombreCompleto, Hash, Salt.

- RF-03: La contraseña de cada usuario debe almacenarse como un hash generado a partir de dicha contraseña, de forma que no sea posible
         desencriptarla ni recuperarla en texto plano.

- RF-04: El hash de la contraseña de cada usuario debe generarse utilizando un salt aleatorio propio de ese usuario, de forma que dos usuarios
         con la misma contraseña tengan hashes distintos entre sí.

- RF-05: La carga de usuarios (RF-02) solo debe estar accesible para usuarios del perfil administrador.

- RF-06: El sistema debe tener una pantalla de inicio de sesión, donde se pida usuario y contraseña, y dicha contraseña debe validarse contra el hash
         asociado al usuario, teniendo en cuenta el salt del mismo usuario.

- RF-07: El sistema debe exigir una sesión autenticada válida (JWT) para acceder a cualquier funcionalidad, con excepción de la pantalla de inicio
         de sesión (RF-06). Toda solicitud a un endpoint protegido de la API sin un token JWT válido debe ser rechazada.

- RF-08: El sistema debe tener una carga de artículos que permita dar de alta, baja y modificación de artículos que incluya los siguientes campos:
         Código, Descripción, Precio de Costo, Margen (%), Precio de Venta (calculado automáticamente según RF-09), Stock Mínimo, Punto de Pedido,
         Stock Ideal.

- RF-09: El sistema debe calcular automáticamente el Precio de Venta de cada artículo a partir del Precio de Costo y el Margen (%), aplicando la
         siguiente fórmula:

         Precio de Venta = Precio de Costo × (1 + Margen / 100)

- RF-10: El sistema debe tener una carga de Movimientos que permita dar de alta, baja y modificación tanto de ventas como de compras, informando
         los siguientes campos en el encabezado: Tipo de Movimiento, Número, y Fecha, y los siguientes campos en el detalle: Código, Cantidad,
         Precio Unitario y Precio Total.

- RF-11: Debe tener una consulta por pantalla, exportable a Excel, llamada "Consulta de Stock Actual" que permita ver la cantidad en existencia actual de
         cada artículo, los parámetros de esta consulta deben ser el rango de artículos (artículo inicial y artículo final) y las columnas de la grilla deben
         ser las siguientes: Código, Descripción y Cantidad.
         La cantidad de cada artículo se calcula del saldo de cada uno, según los movimientos registrados (las ventas restan y las compras suman).

- RF-12: Debe tener una consulta por pantalla, exportable a Excel, llamada "Generar Pedido" que permite ver la cantidad a pedir de cada artículo, los
         parámetros de esta consulta deben ser "solo bajo mínimo" (boolean) y "Modo de Pedido" (lista desplegable, con las siguientes opciones:
         Hasta Stock Mínimo, Hasta Punto Pedido, Hasta Stock Ideal) y las columnas de la grilla deben ser las siguientes: Código, Descripción y
         Cantidad a Pedir. El cálculo de la Cantidad a Pedir para cada una de las 6 combinaciones posibles de estos dos parámetros está
         especificado en AC-23 a AC-28.

- RF-13: Ante cualquier error de ejecución, el mensaje debe guardarse en una tabla de errores, con las siguientes columnas:
         ErrorId (autonumérico), ErrorDateTime, MachineName, Message, FullException.

## Requerimientos No Funcionales

- RNF-01: El Front-End debe ser un sitio Web ASP.NET MVC con .NET 8.

- RNF-02: El Back-End debe estar implementado completamente en una Web API REST con .NET 8, en un proyecto aparte, y con autenticación JWT
          (JSON Web Token), que es invocada desde el Front-End (ver RF-07).

- RNF-03: La base de datos debe ser SQL Server 2017.

- RNF-04: Las consultas "Consulta de Stock Actual" y "Generar Pedido" deben responder en menos de 3 segundos (p95), incluso con hasta 10000 artículos.

- RNF-05: El sistema debe soportar entre 1 y 5 usuarios concurrentes.

- RNF-06: La contraseña de cada usuario debe tener un mínimo de 8 caracteres alfanuméricos.

## Criterios de Aceptación

- AC-01 (RF-01): Dado un registro nuevo de perfil, Cuando se agrega, Entonces queda grabado en la tabla correspondiente.
- AC-02 (RF-01): Dado un registro existente de perfil, Cuando se modifica, Entonces el cambio queda grabado en la tabla correspondiente.
- AC-03 (RF-01): Dado un registro existente de perfil, Cuando se elimina, Entonces deja de existir en la tabla correspondiente.

- AC-04 (RF-02): Dado un registro nuevo de usuario, Cuando se agrega, Entonces queda grabado en la tabla correspondiente.
- AC-05 (RF-02): Dado un registro existente de usuario, Cuando se modifica, Entonces el cambio queda grabado en la tabla correspondiente.
- AC-06 (RF-02): Dado un registro existente de usuario, Cuando se elimina, Entonces deja de existir en la tabla correspondiente.

- AC-07 (RF-03): Dado el alta de un usuario con una contraseña, Cuando se graba el registro, Entonces la contraseña se almacena como hash y no en texto plano ni en un formato reversible.

- AC-08 (RF-04): Dadas dos altas de usuario con la misma contraseña, Cuando se generan sus registros, Entonces los salts grabados para cada usuario son distintos entre sí.

- AC-09 (RF-05): Dado un usuario cuyo perfil no es administrador, Cuando intenta acceder a la carga de usuarios, Entonces el sistema deniega el acceso.

- AC-10 (RF-06): Dado un usuario que no existe, Cuando intenta iniciar sesión, Entonces el sistema muestra el mensaje "Usuario o contraseña incorrectos" y no autoriza el ingreso.
- AC-11 (RF-06): Dado un usuario existente con contraseña incorrecta, Cuando intenta iniciar sesión, Entonces el sistema muestra el mensaje "Usuario o contraseña incorrectos" y no autoriza el ingreso.
- AC-12 (RF-06): Dado un usuario existente con contraseña correcta, Cuando inicia sesión, Entonces el sistema autoriza el ingreso.

- AC-13 (RF-07): Dado un request sin un token JWT válido, Cuando se invoca un endpoint protegido de la API, Entonces el sistema responde con error 401 (No autorizado) y deniega el acceso.

- AC-14 (RF-08): Dado un registro nuevo de artículo, Cuando se agrega, Entonces queda grabado en la tabla correspondiente.
- AC-15 (RF-08): Dado un registro existente de artículo, Cuando se modifica, Entonces el cambio queda grabado en la tabla correspondiente.
- AC-16 (RF-08): Dado un registro existente de artículo, Cuando se elimina, Entonces deja de existir en la tabla correspondiente.

- AC-17 (RF-09): Dado un Precio de Costo y un Margen (%) cargados, Cuando se graba el artículo, Entonces el Precio de Venta se calcula como Precio de Costo × (1 + Margen / 100).

- AC-18 (RF-10): Dado un movimiento nuevo, Cuando se agrega, Entonces queda grabado en las tablas correspondientes (encabezado y detalle).
- AC-19 (RF-10): Dado un movimiento existente, Cuando se modifica, Entonces el cambio queda grabado en las tablas correspondientes (encabezado y detalle).
- AC-20 (RF-10): Dado un movimiento existente, Cuando se elimina, Entonces deja de existir en las tablas correspondientes (encabezado y detalle).

- AC-21 (RF-11): Dado un rango de artículos, Cuando se ejecuta la Consulta de Stock Actual, Entonces devuelve las columnas Código, Descripción y Cantidad, calculada por saldo de movimientos (las ventas restan y las compras suman).
- AC-22 (RF-11): Dado un resultado de la Consulta de Stock Actual, Cuando se presiona el botón "Exportar a Excel", Entonces se descarga un archivo Excel con el contenido de la grilla.

- AC-23 (RF-12): Dado "solo bajo mínimo" = No y Modo de Pedido = "Hasta Stock Mínimo", Cuando se ejecuta Generar Pedido, Entonces para cada artículo la Cantidad a Pedir = MAX(0, Stock Mínimo − Stock Actual).
- AC-24 (RF-12): Dado "solo bajo mínimo" = No y Modo de Pedido = "Hasta Punto Pedido", Cuando se ejecuta Generar Pedido, Entonces para cada artículo la Cantidad a Pedir = MAX(0, Punto de Pedido − Stock Actual).
- AC-25 (RF-12): Dado "solo bajo mínimo" = No y Modo de Pedido = "Hasta Stock Ideal", Cuando se ejecuta Generar Pedido, Entonces para cada artículo la Cantidad a Pedir = MAX(0, Stock Ideal − Stock Actual).
- AC-26 (RF-12): Dado "solo bajo mínimo" = Sí y Modo de Pedido = "Hasta Stock Mínimo", Cuando se ejecuta Generar Pedido, Entonces solo se incluyen los artículos con Stock Actual < Stock Mínimo, y para cada uno la Cantidad a Pedir = Stock Mínimo − Stock Actual.
- AC-27 (RF-12): Dado "solo bajo mínimo" = Sí y Modo de Pedido = "Hasta Punto Pedido", Cuando se ejecuta Generar Pedido, Entonces solo se incluyen los artículos con Stock Actual < Stock Mínimo, y para cada uno la Cantidad a Pedir = Punto de Pedido − Stock Actual.
- AC-28 (RF-12): Dado "solo bajo mínimo" = Sí y Modo de Pedido = "Hasta Stock Ideal", Cuando se ejecuta Generar Pedido, Entonces solo se incluyen los artículos con Stock Actual < Stock Mínimo, y para cada uno la Cantidad a Pedir = Stock Ideal − Stock Actual.
- AC-29 (RF-12): Dado un resultado de la consulta Generar Pedido, Cuando se presiona el botón "Exportar a Excel", Entonces se descarga un archivo Excel con el contenido de la grilla.

- AC-30 (RF-13): Dado un error de ejecución en el sistema, Cuando este ocurre, Entonces sus datos quedan grabados en la tabla de errores.

## Fuera de Alcance
- Queda fuera de alcance el manejo de múltiples proveedores por artículo.
- Queda afuera la generacion de ordenes de compra 

## Riesgos y Dependencias
- Riesgo: Que haya más de 10000 artículos. Mitigación: limitar las consultas con TOP 10000 y agregar filtro opcional por descripción con LIKE '%%'.
- Dependencia: ninguna.
