﻿using System;
using System.Collections.Generic;
using System.Text;
using MicroLib.RabbitMQ.Client.Helper.Standard.Model;
using RabbitMQ.Client;

namespace MicroLib.RabbitMQ.Client.Helper.Standard.Functions
{
    public class RabbitMqFunctions : IRabbitMqFunctions
    {
        public IConnection CreateConnection(ConnectionInputModel connectionInputModel)
        {
            ConnectionFactory _factory;
            IConnection _connection;
            _factory = new ConnectionFactory
            {
                HostName = connectionInputModel.ServerIP,
                //Port = connectionInputModel.ServerPort,
                UserName = connectionInputModel.Username,
                Password = connectionInputModel.Password
            };
            _connection = _factory.CreateConnection(connectionInputModel.ClientName);
            return _connection;
        }


        public bool CreateAndBindExchange(IConnection connection, ExchangeModel exchangeModel, string routeKey, QueueModel queueModel)
        {
            IModel _model;
            _model = connection.CreateModel();

            try
            {
                CreateQueueDeclare(_model, queueModel);
                CreateExchangeDeclare(_model, exchangeModel);

                _model.QueueBind(
                    queueModel.QueueName, 
                    exchangeModel.ExchangeName, 
                    routeKey);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool SendMessage(IConnection connection, string exchangeName, string routeKey, string message)
        {
            IModel _model;
            _model = connection.CreateModel();

            try
            {
                var properties = _model.CreateBasicProperties();
                properties.Persistent = true;

                _model.BasicPublish(exchangeName, routeKey, properties, Encoding.ASCII.GetBytes(message));
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public uint GetMessageCount(IConnection connection, string queueName)
        {
            IModel _model;
            _model = connection.CreateModel();

            var q = CreateQueueDeclare(_model, new QueueModel
            {
                QueueName = queueName
            });
            return q.MessageCount;
        }

        public IEnumerable<string> ReciveMessages(IConnection connection, string queueName, uint msgCount=0)
        {
            if (msgCount == 0)
                msgCount = GetMessageCount(connection, queueName);

            List<string> output = new List<string>();
            IModel _model = connection.CreateModel();

            var consumer = new QueueingBasicConsumer(_model);
            _model.BasicConsume(queueName, true, consumer);

            var count = 0;
            while (count < msgCount)
            {
                output.Add(consumer.Queue.Dequeue().Body.ToString());
                count++;
            }
            return output;
        }




        #region "Helper"
        private QueueDeclareOk CreateQueueDeclare(IModel model, QueueModel queueModel)
        {
            return model.QueueDeclare(
                queueModel.QueueName,
                queueModel.Durable,
                queueModel.Exclusive,
                queueModel.AutoDelete,
                queueModel.Arguments);
        }
        private void CreateExchangeDeclare(IModel model, ExchangeModel exchangeModel)
        {
            model.ExchangeDeclare(
                exchangeModel.ExchangeName,
                exchangeModel.ExchangeType.ToString().ToLower(),
                exchangeModel.Durable,
                exchangeModel.AutoDelete,
                exchangeModel.Arguments);
        }
        #endregion
    }
}