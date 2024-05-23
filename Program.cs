using System.Text;
using System.Text.RegularExpressions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RPCServer.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

//Inicialização da variável para aceder à base de dados
await using var db = new RPCServer_Context();

db.Database.EnsureCreated();


var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

string[] services = {"Servico_A","Servico_B","Servico_C","Servico_D" };
foreach (var service in services)
{
    channel.QueueDeclare(queue: service,
                         durable: false,
                         exclusive: false,
                         autoDelete: false,
                         arguments: null);
}

//channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
var consumer = new EventingBasicConsumer(channel);
foreach (var service in services)
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
            string[] parts = message.Split(' ');
            string username = parts[0];
            string password = parts[1];
            Console.WriteLine($" [.] Username({username})");
            Console.WriteLine($" [.] Password({password})");

            if(Login(username, password) == true)
            {
                response = "Autenticado com sucesso";
            }
            else 
            {
                response = "Credenciais erradas. Ligação ao servidor será encerrada";
            }
            bool ok = Regex.IsMatch(username, "Cl_[0-9][0-9][0-9][0-9]");
            if(ok == false)
            {
                message = Encoding.UTF8.GetString(body);
                if (message == "1")
                {
                foreach (var item in ListarServicos(username))
                {
                    response = item;
                }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($" [.] {e.Message}");
            response = string.Empty;
        }
        finally
        {
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

bool Login(string user, string password)
{
    
    if(db.Admins.Any(c => c.Username == user && c.Password == password) == true || db.Clientes.Any(c => c.ClienteId == user) == true)
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

List<string> ListarServicos(string username)
{
    List<string> list = new List<string>();
    var servicos = db.Servicos.Where(s => s.AdminName == username).ToList();
    foreach (var servico in servicos)
    {
        list.Add(servico.Descricao+ " alocaoda" + servico.ClienteId + "->" + servico.Estado);
    }
    return list;
}

//TODAS AS FUNÇÕES DO SERVIÇO
//ADMIN: InserirTarefa;ListarServiços;UpdateTarefa;
//GERAL: LOGIN
//Garantir que cada cliente (mota e Administrador) esteja sempre associado a um
//único serviço.