# ZPL Network Printer

Serviço Windows para monitoramento de arquivos em formato ZPL e emissão em impressoras Zebra local ou compartilhada na rede.


## Preparação

Verifique o arquivo "zplnetconfig.xml" na pasta onde encontra-se o executável, para que o serviço posso acessa-lo. Nesse arquivo devem conter as configurações.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<FolderCollection>
  <Folders>
    <Folder>
      <Name>C:\\MONITOR\\PRN1</Name>
      <Printer>\\192.168.2.11\S4M</Printer>
      <Port></Port>
      <Method>Copy</Method>
    </Folder>
    <Folder>
      <Name>C:\\MONITOR\\PRN2</Name>
      <Printer>192.168.2.14</Printer>
      <Port>6100</Port>
      <Method>TCP</Method>
    </Folder>
  </Folders>
</FolderCollection>
```

## Tipos de Emissão

O programa irá enviar os dados para a impressora com os seguintes métodos.

```bash
Print = Realiza a impressão padrão na impressora configurada no sistema.
TCP   = Realiza a impressão através de uma conexão TCP/IP diretamente com o dispositivo.
Copy  = Realiza a impressão copiando o arquivo ZPL para o caminho de compartilhamento da impressora.
```
