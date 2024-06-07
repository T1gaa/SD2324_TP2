using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RPCServer.Data;


//Inicialização da variável para aceder à base de dados
await using var db = new RPCServer_Context();

db.Database.EnsureCreated();


var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();
using var channel1 = connection.CreateModel();


channel1.QueueDeclare(queue: "Tarefas",
                      durable: false,
                      exclusive: false,
                      autoDelete: false,
                      arguments: null);
//CRIAR QUEUES DE COMUNICAÇÃO ENTRE CLIENTE E SERVIDOR E QUEUE DE ADMIN SERVIDOR
channel1.ExchangeDeclare(exchange: "NotificacaoTarefa", type: ExchangeType.Topic);


string[] queues = {"RPC_Cliente","RPC_Admin" };

foreach (var service in queues)
{
    channel.QueueDeclare(queue: service,
                         durable: false,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);
}

channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

var consumer = new EventingBasicConsumer(channel);
foreach (var service in queues)
{
    channel.BasicConsume(queue: service,
                         autoAck: false,
                         consumer: consumer);
}

consumer.Received += (model, ea) =>
{
    string response = string.Empty;

    var body = ea.Body.ToArray();
    var props = ea.BasicProperties;
    var replyProps = channel.CreateBasicProperties();
    replyProps.CorrelationId = props.CorrelationId;

    try
        {
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($" [.] ({message})");
            
            switch(message)
            {
                case { } when  message.StartsWith('1'):
                    bool ok = Regex.IsMatch(UserSession.Username, "Cl_[0-9][0-9][0-9][0-9]");
                    if (ok == false)
                    {
                        response = ListarServicos();
                    }
                    else
                    {
                        response = ConcluirServico();
                    }
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    channel.BasicPublish(exchange: string.Empty,
                                     routingKey: props.ReplyTo,
                                     basicProperties: replyProps,
                                     body: responseBytes);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    break;
                case { } when message.StartsWith('2'):
                    bool test = Regex.IsMatch(UserSession.Username, "Cl_[0-9][0-9][0-9][0-9]");
                    if (test == false)
                    {
                        string[] parts = AdicionarTarefa(message).Split(" ");
                        string servicotipo = parts[1] + parts[2];

                        string queuenotif = "Nova tarefa disponível: " + parts[0];
                        var queuenotifBytes = Encoding.UTF8.GetBytes(queuenotif);
                        switch (servicotipo)
                        {
                            case "ServicoA":
                                channel1.BasicPublish(exchange: "NotificacaoTarefa",
                                    routingKey: "ServicoA",
                                    basicProperties: null,
                                    body: queuenotifBytes);

                                Console.WriteLine($"[x] Sent Serviço A:'{queuenotif}'");
                            break;
                            case "ServicoB":
                                channel1.BasicPublish(exchange: "NotificacaoTarefa",
                                    routingKey: "ServicoB",
                                    basicProperties: null,
                                    body: queuenotifBytes);

                                Console.WriteLine($"[x] Sent Serviço B:'{queuenotif}'");
                            break;
                            case "ServicoC":
                                channel1.BasicPublish(exchange: "NotificacaoTarefa",
                                    routingKey: "ServicoC",
                                    basicProperties: null,
                                    body: queuenotifBytes);

                                Console.WriteLine($"[x] Sent Serviço C:'{queuenotif}'");
                            break;
                            case "ServicoD":
                                channel1.BasicPublish(exchange: "NotificacaoTarefa",
                                    routingKey: "ServicoD",
                                    basicProperties: null,
                                    body: queuenotifBytes);

                                Console.WriteLine($"[x] Sent Serviço D:'{queuenotif}'");
                            break;
                            default:
                            break;
                        }
                        response = string.Join(" ", parts.Skip(3));
                        responseBytes = Encoding.UTF8.GetBytes(response);
                        channel.BasicPublish(exchange: string.Empty,
                                     routingKey: props.ReplyTo,
                                     basicProperties: replyProps,
                                     body: responseBytes);
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                    else
                    {
                        string[] parts = message.Split(" ");
                        response = RequererTarefa(parts[1]);
                    }
                break;
                case { } when message.StartsWith('3'):
                    response = AlocarClienteServico(message);
                    responseBytes = Encoding.UTF8.GetBytes(response);
                    channel.BasicPublish(exchange: string.Empty,
                                     routingKey: props.ReplyTo,
                                     basicProperties: replyProps,
                                     body: responseBytes);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                break;
                case { } when message.StartsWith('4'):
                    response = DessassociarServico();
                    responseBytes = Encoding.UTF8.GetBytes(response);
                    channel.BasicPublish(exchange: string.Empty,
                                     routingKey: props.ReplyTo,
                                     basicProperties: replyProps,
                                     body: responseBytes);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    break;
                case { } when message.StartsWith('5'):
                    response = AssociarServico();
                    responseBytes = Encoding.UTF8.GetBytes(response);
                    channel.BasicPublish(exchange: string.Empty,
                                     routingKey: props.ReplyTo,
                                     basicProperties: replyProps,
                                     body: responseBytes);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    break;
                case { } when message.StartsWith('6'):
                    response = ModificarServico(message);
                    responseBytes = Encoding.UTF8.GetBytes(response);
                    channel.BasicPublish(exchange: string.Empty,
                                 routingKey: props.ReplyTo,
                                 basicProperties: replyProps,
                                 body: responseBytes);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    break;
            default:
                    if (Login(message) == true)
                    {
                        response = "Autenticado com sucesso";
                    }
                    else
                    {
                        response = ("Credenciais erradas");
                        
                    }
                    responseBytes = Encoding.UTF8.GetBytes(response);
                    channel.BasicPublish(exchange: string.Empty,
                                     routingKey: props.ReplyTo,
                                     basicProperties: replyProps,
                                     body: responseBytes);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    break;
            }                
        }
        catch (Exception e)
        {
            Console.WriteLine($" [.] {e.Message}");
            response = string.Empty;
            var responseBytes = Encoding.UTF8.GetBytes(response);
            channel.BasicPublish(exchange: string.Empty,
                             routingKey: props.ReplyTo,
                             basicProperties: replyProps,
                             body: responseBytes);
            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        }
};

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();

bool Login(string user)
{
    string[] parts = user.Split(' ');
    UserSession.Username = parts[0];
    string password = parts[1];

    if (db.Admins.Any(c => c.Username == UserSession.Username && c.Password == password) == true || db.Clientes.Any(c => c.ClienteId == UserSession.Username && c.Password == password) == true)
    {
        return true;
    }
    else 
    {
        return false;
    }
    //Se devolver true login com sucesso
    //Se devolver false login sem sucesso
}

string ListarServicos()
{
    var admin = db.Admins.First(a => a.Username == UserSession.Username);

    var servicoadmin = admin.Servico;

    var servicos = db.Servicos.Where(s => s.ServicoTipo == servicoadmin).ToList();

    var servicosConcatenados = string.Join("; ", servicos.Select(s => $"{s.ServicoId}: {s.Descricao} ({s.Estado})"));

    return servicosConcatenados;
}

string AdicionarTarefa(string username)
{
    string[] parts = username.Split(" ");
    string tarefaId = parts[1];
    string descricao = string.Join(" ", parts.Skip(2)); ;
    var servicotipo = db.Admins.First(s => s.Username == UserSession.Username).Servico;
    var tarefa = new Servico
    {
        ServicoId = tarefaId,
        ServicoTipo = servicotipo,
        Descricao = descricao,
        Estado = "Nao alocado",
        AdminName = UserSession.Username
    };

    db.Servicos.Add(tarefa);
    db.SaveChanges();

    return tarefaId + " " + servicotipo + " " + descricao+ " " + "Tarefa adicionada com sucesso";
}

string AlocarClienteServico(string username)
{
    string[] parts = username.Split(" ");
    string cliente = parts[1];

    var cl = db.Clientes.FirstOrDefault(s => s.ClienteId == cliente);
    cl.Servico = db.Admins.FirstOrDefault(s => s.Username == UserSession.Username).Servico;

    db.SaveChanges();

    return $"Cliente {cliente} associado com sucesso {cl.Servico}";
}
 
string DessassociarServico()
{
    var admin = db.Admins.FirstOrDefault(s => s.Username == UserSession.Username);
    admin.Servico = "";
    foreach (var servico in db.Servicos.Where(s => s.AdminName == UserSession.Username))
    {
        servico.AdminName = "";
    }

    db.SaveChanges();

    return "Dessassociação do serviço com sucesso";
}

string AssociarServico()
{
    var admin = db.Admins.FirstOrDefault(s => s.Username == UserSession.Username);
    if (admin.Servico == null || admin.Servico == "")
    {
        var servico = db.Servicos.FirstOrDefault(s => s.AdminName == "");
        servico.AdminName = UserSession.Username;

        var tiposervico = servico.ServicoTipo;
        foreach (var item in db.Servicos.Where(S => S.ServicoTipo == tiposervico))
        {
            item.AdminName = UserSession.Username;
        }


        admin.Servico = tiposervico;

        db.SaveChanges();

        return $"Associado ao serviço {tiposervico}";
    }
    else
    {
        return "Já está associado a um serviço";
    }
}

string ModificarServico(string message)
{
    string[] parts = message.Split(',');
    var idTarefa = parts[1];
    var descricao = string.Join(" ", parts.Skip(1));
    var servico = db.Servicos.First(s => s.ServicoId == idTarefa);
    servico.Descricao = descricao;

    return $"Descrição do {servico.ServicoId} mudada com sucesso";
}

string ConcluirServico()
{
    var servico = db.Servicos.FirstOrDefault(s => s.ClienteId == UserSession.Username);

    servico.Estado = "Concluido";
    servico.ClienteId = "";

    db.SaveChanges();

    return "O estado da sua tarefa foi atualizado com sucesso";
    
}

string RequererTarefa(string idTarefa)
{
    var tarefa = db.Servicos.First(s => s.ServicoId == idTarefa);
    if(tarefa.ClienteId == null || tarefa.ClienteId == "")
    {
        tarefa.ClienteId = UserSession.Username;
        tarefa.Estado = "Em curso";

        return $"Subscreveu com sucesso a tarefa: {tarefa.ServicoId}{tarefa.Descricao}";
    }
    else
    {
        return "Esta tarefa já está alocada a outro cliente";
    }
}
public static class UserSession
{
    public static string Username { get; set; }
}
//TODAS AS FUNÇÕES DO SERVIÇO
//ADMIN: UpdateTarefa;
//Garantir que cada cliente (mota e Administrador) esteja sempre associado a um único serviço.