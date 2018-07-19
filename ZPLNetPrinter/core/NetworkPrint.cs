using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Serialization;
using ZPLNetPrinter.core;

namespace ZPLNetPrinter
{
    class NetworkPrint
    {
        private FolderCollection folders = null;

        public NetworkPrint()
        {
        }

        public Tuple<bool,string> LoadConfiguration()
        {
            bool bResult   = true;
            string sResult = "";

            try
            {
                // Cria as classes de leitura do XML
                XmlSerializer serializer = new XmlSerializer(typeof(FolderCollection));
                StreamReader reader = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"zplnetconfig.xml"));
                // ---

                // Lê o XML de configuração com as pastas a serem verificadas
                folders = (FolderCollection)serializer.Deserialize(reader);

                // Fecha a classe do reader
                reader.Close();
            }
            catch (Exception e)
            {
                bResult = false;
                sResult = e.ToString();
            }

            return new Tuple<bool,string>(bResult, sResult);
        }

        public string[] SearchLabels()
        {
            string targetFolder;
            List<string> results = new List<string>();

            for (int i = 0; i < this.folders.Folder.Length; i++)
            {
                // Cria a classe de Pasta e armazena a entrada atual do loop
                Folder folder = new Folder();
                folder = this.folders.Folder[i];
                // ---

                // Verifica se o diretório existe
                if (Directory.Exists(folder.Name)) {
                    // Armazena o nome dos arquivos na pasta atual
                    string[] fileEntries = Directory.GetFiles(folder.Name);

                    // Loop nos arquivos da pasta
                    foreach (string fileName in fileEntries)
                    {
                        // Verifica se a extensão do arquivo é TXT
                        if (Path.GetExtension(fileName).ToLower() == ".txt")
                        {
                            // Move o arquivo para a pasta de Processado
                            targetFolder = "Processado";

                            // Case no métodos de emissão possíveis
                            switch (folder.Method)
                            {
                                case "Print":
                                    var resultPrint = SendDataPrinter(folder.Printer, fileName);

                                    // Verifica se houve erro na impressão por TCP
                                    if (!resultPrint.Item1)
                                    {
                                        results.Add(string.Format("Error in method Print: {0}", resultPrint.Item2));
                                        targetFolder = "Erro";
                                    }
                                    // ---

                                    break;
                                case "TCP":
                                    var resultTCP = SendDataTCP(folder.Printer, Convert.ToInt32(folder.Port), fileName);

                                    // Verifica se houve erro na impressão por TCP
                                    if (!resultTCP.Item1)
                                    {
                                        results.Add(string.Format("Error in method TCP: {0}", resultTCP.Item2));
                                        targetFolder = "Erro";
                                    }
                                    // ---

                                    break;
                                case "Copy":
                                    // Copia o arquivo a ser impresso diretamente na porta da impressora
                                    var resultCopy = CopyFileTo(folder.Name, folder.Printer, Path.GetFileName(fileName), false, true);

                                    // Verifica se houve erro na cópia
                                    if (!resultCopy.Item1)
                                    {
                                        results.Add(string.Format("Error in method copy: {0}", resultCopy.Item2));
                                        targetFolder = "Erro";
                                    }
                                    // ---

                                    break;
                                default:
                                    // Essa opção ser ativada quando não for possível encontrar nenhum método para a emissão dos arquivos
                                    results.Add(string.Format("Method not found for folder name '{0}' into configuration file.", folder.Name));
                                    targetFolder = "Erro";
                                    break;
                            } 
                            // --- (Fim do Case no métodos de emissão possíveis)
                        }
                        else
                        {
                            targetFolder = "Invalido";
                        }

                        // Move o arquivo para a pasta de acordo com a ocorrência acima
                        CopyFileTo(folder.Name, targetFolder, Path.GetFileName(fileName), true);
                    }
                    // --- (Fim do Loop nos arquivos da pasta)
                }
                else
                {
                    // Essa opção ser ativada quando não for possível encontrar nenhum método para a emissão dos arquivos
                    results.Add(string.Format("Directory '{0}' not found.", folder.Name));
                }
                // -- (Fim do Verifica se o diretório existe)
            }

            // Retorna o array de resultados (caso ocorram erros)
            return results.ToArray();
        }

        private Tuple<bool,string> CopyFileTo(string fromFolder, string toFolder, string fileName, bool deleteSource = false, bool toPrinter = false)
        {
            bool   bResult = true;
            string sResult = "";
            string pathSep = Path.DirectorySeparatorChar.ToString();

            // Corrige o final do path com a barra ("\")
            if (!fromFolder.EndsWith(pathSep)) { fromFolder += pathSep; }

            // Verifica se não se trata de um comando para impressora
            if (!toPrinter)
            {
                // Prepara o pasta destino com o oath absoluto
                toFolder = fromFolder + toFolder + pathSep;

                // Verifica se o diretório destino do arquivo não existe
                if (!Directory.Exists(toFolder))
                {
                    // Cria o diretório de destino do arquivo
                    Directory.CreateDirectory(toFolder);
                }
                // ---

                // Prepara o path destino completo
                toFolder = Path.Combine(toFolder, fileName);
            }
            // ---

            // Inicializa a tratativa de erro
            try
            {
                // Prepara o arquivo origem para cópia
                fromFolder = Path.Combine(fromFolder, fileName);

                // Copia o arquivo para a pasta destino
                File.Copy(fromFolder, toFolder, true);

                // Verifica se precisa excluir o arquivo destino
                if (deleteSource) { File.Delete(fromFolder); }
            }
            catch (Exception e)
            {
                sResult = e.Source + " - Error: " + e;
                bResult = false;
            }
            // ---

            // Retorna os resultados da função
            return new Tuple<bool, string>(bResult, sResult);
        }

        private Tuple<bool, string> SendDataTCP(string IP, int Port, string fileName)
        {
            bool bResult   = true;
            string sResult = "";

            NetworkStream ns = null;
            Socket socket    = null;

            // Verifica se o arquivo existe
            if (File.Exists(fileName))
            {
                // Inicializa a tratativa de erro
                try
                {
                    // Faz a leitura completa do arquivo a ser impresso
                    string sData = File.ReadAllText(fileName);

                    // Prepara as configurações da impressora (IP/Porta)
                    IPEndPoint printerIP = new IPEndPoint(IPAddress.Parse(IP), Port);

                    // Cria o objeto do socket de comunicação
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(printerIP);
                    // ---

                    // Cria o objeto de streamming de rede 
                    ns = new NetworkStream(socket);

                    // Envia os dados do arquivo para a impressora
                    byte[] toSend = Encoding.ASCII.GetBytes(sData);
                    ns.Write(toSend, 0, toSend.Length);
                    // ---
                }
                catch (Exception e)
                {
                    bResult = false;
                    sResult = e.ToString();
                }
                finally
                {
                    // Fecha a classe de stream de rede
                    if (ns != null) { ns.Close(); }

                    // Fecha a classe de socket
                    if (socket != null && socket.Connected) { socket.Close(); }
                }
                // ---
            }
            else
            {
                // Atualiza as informações de resultado
                bResult = false;
                sResult = string.Format("File '{0}' not found.", fileName);
                // ---
            }

            // Retorna o resultado do método
            return new Tuple<bool, string>(bResult, sResult);
        }
        private Tuple<bool, string> SendDataPrinter(string printerName, string fileName)
        {
            bool bResult = true;
            string sResult = "";

            // Verifica se o arquivo existe
            if (File.Exists(fileName))
            {
                // Inicializa a tratativa de erro
                try
                {
                    // Faz a leitura completa do arquivo a ser impresso
                    string sData = File.ReadAllText(fileName);

                    // Cria o objeto de comunicação com a impressora   
                    RawPrinterHelper.SendStringToPrinter(printerName, sData);
                }
                catch (Exception e)
                {
                    bResult = false;
                    sResult = e.ToString();
                }
                // ---
            }
            else
            {
                // Atualiza as informações de resultado
                bResult = false;
                sResult = string.Format("File '{0}' not found.", fileName);
                // ---
            }

            // Retorna o resultado do método
            return new Tuple<bool, string>(bResult, sResult);
        }

    }
}
