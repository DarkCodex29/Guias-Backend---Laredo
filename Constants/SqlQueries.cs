namespace GuiasBackend.Constants
{
    /// <summary>
    /// Repositorio centralizado de consultas SQL para mantener consistencia y facilitar mantenimiento
    /// </summary>
    public static class SqlQueries
    {
        // Consultas para Usuario
        public const string GetAllUsuarios = @"
            SELECT * 
            FROM (
                SELECT u.*, ROWNUM rn
                FROM (
                    SELECT * 
                    FROM USUARIO
                    ORDER BY NOMBRES
                ) u
                WHERE ROWNUM <= :endRow
            )
            WHERE rn > :startRow";
        
        public const string GetUsuarioById = "SELECT * FROM USUARIO WHERE ID = :id";
        public const string GetUsuarioByUsername = "SELECT * FROM USUARIO WHERE USERNAME = :username";
        public const string GetUsuarioByEmail = "SELECT * FROM USUARIO WHERE EMAIL = :email";
        public const string ExisteUsuario = "SELECT COUNT(*) FROM USUARIO WHERE USERNAME = :username";
        public const string ExisteEmail = "SELECT COUNT(*) FROM USUARIO WHERE EMAIL = :email";
        public const string CountUsuarios = "SELECT COUNT(*) FROM USUARIO";

        // Consultas para VistaCampo
        public const string GetAllCampos = @"
            SELECT NVL(CAMPO, '') as CAMPO, 
                   NVL(DESC_CAMPO, '') as DESC_CAMPO
            FROM (
                SELECT t.*, ROWNUM rn
                FROM (
                    SELECT CAMPO, DESC_CAMPO 
                    FROM PIMS_GRE.VISTA_CAMPO 
                    ORDER BY CAMPO NULLS LAST
                ) t
                WHERE ROWNUM <= :endRow
            )
            WHERE rn > :startRow";

        public const string GetCampoByCodigo = "SELECT NVL(CAMPO, '') as CAMPO, NVL(DESC_CAMPO, '') as DESC_CAMPO FROM PIMS_GRE.VISTA_CAMPO WHERE CAMPO = :campo_param";
        public const string GetCampoByDescripcion = "SELECT NVL(CAMPO, '') as CAMPO, NVL(DESC_CAMPO, '') as DESC_CAMPO FROM PIMS_GRE.VISTA_CAMPO WHERE DESC_CAMPO LIKE :desc_param";
        public const string ExisteCampo = "SELECT 1 FROM PIMS_GRE.VISTA_CAMPO WHERE CAMPO = :campo_param AND ROWNUM = 1";
        
        // Consultas para VistaCuartel
        public const string GetAllCuarteles = @"
            SELECT NVL(CAMPO, '') as CAMPO, 
                   NVL(JIRON, '') as JIRON, 
                   NVL(CUARTEL, '') as CUARTEL
            FROM (
                SELECT t.*, ROWNUM rn
                FROM (
                    SELECT CAMPO, JIRON, CUARTEL 
                    FROM PIMS_GRE.VISTA_CUARTEL 
                    ORDER BY CAMPO, JIRON, CUARTEL NULLS LAST
                ) t
                WHERE ROWNUM <= :endRow
            )
            WHERE rn > :startRow";
            
        public const string GetCuartelByCuartel = "SELECT NVL(CAMPO, '') as CAMPO, NVL(JIRON, '') as JIRON, NVL(CUARTEL, '') as CUARTEL FROM PIMS_GRE.VISTA_CUARTEL WHERE CUARTEL = :cuartel_param";
        public const string GetCuartelesByCampo = @"
            SELECT CAMPO, JIRON, CUARTEL
            FROM (
                SELECT t.*, ROWNUM rn
                FROM (
                    SELECT CAMPO, JIRON, CUARTEL 
                    FROM PIMS_GRE.VISTA_CUARTEL 
                    WHERE CAMPO = :campo_param
                    ORDER BY JIRON, CUARTEL
                ) t
                WHERE ROWNUM <= :endRow
            )
            WHERE rn > :startRow";
            
        // Consultas para VistaJiron
        public const string GetAllJirones = @"
            SELECT NVL(CAMPO, '') as CAMPO, 
                   NVL(JIRON, '') as JIRON
            FROM (
                SELECT t.*, ROWNUM rn
                FROM (
                    SELECT CAMPO, JIRON
                    FROM PIMS_GRE.VISTA_JIRON 
                    ORDER BY CAMPO, JIRON NULLS LAST
                ) t
                WHERE ROWNUM <= :endRow
            )
            WHERE rn > :startRow";
            
        public const string GetJironByJiron = "SELECT NVL(CAMPO, '') as CAMPO, NVL(JIRON, '') as JIRON FROM PIMS_GRE.VISTA_JIRON WHERE JIRON = :jiron_param";
        public const string GetJironesByCampo = @"
            SELECT CAMPO, JIRON
            FROM (
                SELECT t.*, ROWNUM rn
                FROM (
                    SELECT CAMPO, JIRON
                    FROM PIMS_GRE.VISTA_JIRON 
                    WHERE CAMPO = :campo_param
                    ORDER BY JIRON
                ) t
                WHERE ROWNUM <= :endRow
            )
            WHERE rn > :startRow";
            
        // Consultas para VistaEmpleado
        public const string GetAllEmpleados = @"
            SELECT CODIGO, 
                   NVL(EMPLEADO, '') as ""EMPLEADO"", 
                   NVL(DNI, '') as ""DNI"", 
                   CD_TRANSP
            FROM (
                SELECT t.*, ROWNUM rn
                FROM (
                    SELECT CODIGO, EMPLEADO, DNI, CD_TRANSP 
                    FROM PIMS_GRE.VISTA_EMPLEADO 
                    ORDER BY EMPLEADO NULLS LAST
                ) t
                WHERE ROWNUM <= :endRow
            )
            WHERE rn > :startRow";
            
        public const string GetEmpleadoByDni = "SELECT CODIGO, NVL(EMPLEADO, '') as \"EMPLEADO\", NVL(DNI, '') as \"DNI\", CD_TRANSP FROM PIMS_GRE.VISTA_EMPLEADO WHERE DNI = :dni_param";
        public const string GetEmpleadoByEmpleado = "SELECT CODIGO, NVL(EMPLEADO, '') as \"EMPLEADO\", NVL(DNI, '') as \"DNI\", CD_TRANSP FROM PIMS_GRE.VISTA_EMPLEADO WHERE EMPLEADO = :empleado_param";
        
        // Consultas para Guia
        public const string GetGuiasByUsuario = "SELECT * FROM PIMS_GRE.GUIAS WHERE ID_USUARIO = :id_usuario_param ORDER BY FECHA_SUBIDA DESC";
        public const string ExisteGuiaByNombre = "SELECT 1 FROM PIMS_GRE.GUIAS WHERE NOMBRE = :nombre_param AND ROWNUM = 1";
        
        // Consultas para VistaTransportista
        public const string GetAllTransportistas = @"
            SELECT COD_TRANSP, 
                   NVL(TRANSPORTISTA, '') as TRANSPORTISTA, 
                   NVL(RUC, '') as RUC
            FROM (
                SELECT t.*, ROWNUM rn
                FROM (
                    SELECT COD_TRANSP, TRANSPORTISTA, RUC 
                    FROM PIMS_GRE.VISTA_TRANSPORTISTA 
                    ORDER BY TRANSPORTISTA NULLS LAST
                ) t
                WHERE ROWNUM <= :endRow
            )
            WHERE rn > :startRow";
            
        // Consultas para VistaEquipo
        public const string GetAllEquipos = @"
            SELECT COD_EQUIPO, NVL(PLACA, '') as PLACA, COD_TRANSP, NVL(TIP_EQUIPO, '') as TIP_EQUIPO
            FROM (
                SELECT t.*, ROWNUM rn
                FROM (
                    SELECT COD_EQUIPO, PLACA, COD_TRANSP, TIP_EQUIPO 
                    FROM PIMS_GRE.VISTA_EQUIPOS 
                    ORDER BY COD_EQUIPO
                ) t
                WHERE ROWNUM <= :endRow
            )
            WHERE rn > :startRow";
        
        public const string GetEquipoByCodEquipo = @"
            SELECT COD_EQUIPO, NVL(PLACA, '') as PLACA, COD_TRANSP, NVL(TIP_EQUIPO, '') as TIP_EQUIPO 
            FROM PIMS_GRE.VISTA_EQUIPOS 
            WHERE COD_EQUIPO = :cod_equipo_param";

        public const string GetEquipoByPlaca = @"
            SELECT COD_EQUIPO, NVL(PLACA, '') as PLACA, COD_TRANSP, NVL(TIP_EQUIPO, '') as TIP_EQUIPO 
            FROM PIMS_GRE.VISTA_EQUIPOS 
            WHERE PLACA = :placa_param";

        public const string GetEquiposByCodTransp = @"
            SELECT COD_EQUIPO, NVL(PLACA, '') as PLACA, COD_TRANSP, NVL(TIP_EQUIPO, '') as TIP_EQUIPO 
            FROM PIMS_GRE.VISTA_EQUIPOS 
            WHERE COD_TRANSP = :cod_transp_param";
    }
}
