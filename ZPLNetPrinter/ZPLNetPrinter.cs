using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Runtime.InteropServices;

namespace ZPLNetPrinter
{
    public partial class ZPLNetPtrinter : ServiceBase
    {

        private System.Diagnostics.EventLog eventLog1;
        private System.Timers.Timer timer;
        private int eventId = 1;

        public enum ServiceState
        {
            SERVICE_STOPPED             = 0x00000001,
            SERVICE_START_PENDING       = 0x00000002,
            SERVICE_STOP_PENDING        = 0x00000003,
            SERVICE_RUNNING             = 0x00000004,
            SERVICE_CONTINUE_PENDING    = 0x00000005,
            SERVICE_PAUSE_PENDING       = 0x00000006,
            SERVICE_PAUSED              = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        }

        public ZPLNetPtrinter()
        {
            InitializeComponent();

            eventLog1 = new System.Diagnostics.EventLog();

            if (!System.Diagnostics.EventLog.SourceExists("ZPLNetworkPrinter"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "ZPLNetworkPrinter", "Application");
            }

            eventLog1.Source = "ZPLNetworkPrinter";
            eventLog1.Log    = "Application";
        }

        protected override void OnStart(string[] args)
        {
            // Atualiza o status do serciço para "Inicialização pendente"
            ServiceStatus serviceStatus  = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint     = 100000;

            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            // ---

            // Cria um registro no Log de eventos
            eventLog1.WriteEntry("Starting ZPL network printer.");

            // Cria o timer para processamento em 1 minuto
            timer = new System.Timers.Timer();
            timer.Interval = 60000; // 60 seconds  
            timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
            timer.Start();
            // ---

            // Atualiza o status do serciço para "Iniciado"
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            // ---
        }

        protected override void OnStop()
        {
            // Atualiza o status do serciço para "Encerramento pendente"
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;

            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            // ---

            // Cria um registro no Log de eventos
            eventLog1.WriteEntry("Stopping ZPL network printer.", EventLogEntryType.Warning);

            // Para o timer
            timer.Stop();
            // ---

            // Atualiza o status do serciço para "Parado"
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            // ---
        }

        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            // Cria a classe de monitoramento e emissão de etiquetas
            NetworkPrint networkPrint = new NetworkPrint();

            // Faz a carga das configurações do serviço
            var vResult = networkPrint.LoadConfiguration();

            // Verifica se houve erro na carga das configurações
            if (!vResult.Item1) {
                eventLog1.WriteEntry("Error loading configuration file: " + vResult.Item2, EventLogEntryType.Error, eventId++);
            }
            else
            {
                // Busca por etiquetas pendentes de emissão
                string[] printResults = networkPrint.SearchLabels();

                // Verifica se houveram erros durante o processo de emissão
                if (printResults.Length > 0)
                {
                    // Loop para enviar os erros para o Log de Eventos
                    for (int i = 0; i < printResults.Length; i++)
                    {
                        eventLog1.WriteEntry(printResults[i], EventLogEntryType.Error, eventId++);
                    }
                    // ---
                }
                // ---
            }
            // --- (Fim do Verifica se houve erro na carga das configurações)

        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);
    }
}
