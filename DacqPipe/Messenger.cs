using System;
using System.Collections.Generic;

using System.Text;
using ZMQ;
using System.Threading;
using System.Collections;
using System.Configuration;
using log4net;
using log4net.Config;

//using Apache.NMS;
//using Apache.NMS.ActiveMQ;
//using Apache.NMS.ActiveMQ.Commands;
using System.IO;
using Latino;

namespace Messaging
{
    public class Messenger
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(Messenger));

        //queue variables
        private BlockingQueue<String> outgoingMessageQueue; //keeps messages will be sent via zeromq
        private BlockingQueue<String> incomingMessageQueue; //keeps messages will be received via zeromq
        private BlockingQueue<String> loggerQueue; //keeps messages will be sent via zeromq
        private BlockingQueue<String> brokerQueue; //keeps messages will be sent via broker
        private BlockingQueue<String> fileQueue; //keeps file names

        //file storage variables
        private int fileNum = 1; //keeps file counter
        private int inFileNum = 1; //keeps file counter
        private String inFileStorageAddress; //file storage folder
        private String outFileStorageAddress; //file storage folder
        private int maxFileStorageNum; //max number of messages can be stored in files

        //zeromq variables
        private Socket sender; //connection to send messages
        private Socket receiver; //connection to receive messages
        private Socket lbReceiver; //connection to receive overflow messages
        private Socket lbSender; //connection to receive overflow messages
        private Socket finishReceiver; //connection to receive overflow messages
        private Socket finishSender; //connection to receive overflow messages
        private Context zeromqContext;

        //channel for logging output (DB_LOGGING)
        private Socket loggerEmitter;

        //activemq variables
        //private IMessageProducer activemqSender;
        //private Apache.NMS.IConnection activeMQconnection;

        //zeromq balancing commands
        private static String WAIT_COMMAND = "WAIT";
        private static String FINISH_COMMAND = "FINISH";
        private static String CONTINUE_COMMAND = "CONTINUE";
        private static String MESSAGE_REQUEST = "R";

        //zeromq pipeline or request & reply
        private static int MESSAGING_TYPE = 1;
        private const int PIPELINE = 0;
        private const int REQ_REP = 1;

        private static int IGNORE_QUEUE_OVERFLOW = 1;
        private static int MAX_QUEUE_SIZE = 10;
        private static int MIN_QUEUE_SIZE = 1;//min size to start sending messages
        private static int MAX_BROKER_QUEUE_SIZE = 4;

        //broker variables
        private static int BROKER = 0;
        private const int NONE = 0;
        private const int ACTIVEMQ = 1;

        private const int ON = 1;
        private const int OFF = 0;

        private Thread zerommqSendThread;
        private Thread zerommqReceiveThread;
        private Thread loggerThread;
        private Thread brokerThread;
        private Thread fileThread;
        private Thread finishThread;

        private bool messagingFinished = false;

        private static bool BLOCKING_QUEUE = false;

        //logging flag
        private static bool DB_LOGGING = true;

        //used for load balancing
        private static int ID = 1;
        private static int producerNum = 1;
        private static int receiverNum = 1;
        private static float ratio = 1;
        Set<int> lbSet = new Set<int>();

        public Messenger()
        {
            initLogger();

            //zeromq opening connections
            outgoingMessageQueue = new BlockingQueue<String>();
            incomingMessageQueue = new BlockingQueue<String>();
            brokerQueue = new BlockingQueue<String>();
            fileQueue = new BlockingQueue<String>();
            loggerQueue = new BlockingQueue<String>();
            zeromqContext = new Context(1);
            //reading parameters from the configuration file
            MESSAGING_TYPE = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MessagingType"));
            MAX_QUEUE_SIZE = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MAX_QUEUE_SIZE"));
            MIN_QUEUE_SIZE = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MIN_QUEUE_SIZE"));
            IGNORE_QUEUE_OVERFLOW = Convert.ToInt32(ConfigurationManager.AppSettings.Get("IGNORE_QUEUE_OVERFLOW"));
            BROKER = Convert.ToInt32(ConfigurationManager.AppSettings.Get("Broker"));
            String addressSend = ConfigurationManager.AppSettings.Get("MessageSendAddress");
            String addressReceive = ConfigurationManager.AppSettings.Get("MessageReceiveAddress");
            inFileStorageAddress = ConfigurationManager.AppSettings.Get("InFileStorageAddress");
            outFileStorageAddress = ConfigurationManager.AppSettings.Get("OutFileStorageAddress");
            maxFileStorageNum = Convert.ToInt32(ConfigurationManager.AppSettings.Get("MAX_FILE_STORAGE_SIZE"));
            WAIT_COMMAND = ConfigurationManager.AppSettings.Get("WAIT_COMMAND");
            FINISH_COMMAND = ConfigurationManager.AppSettings.Get("FINISH_COMMAND");
            CONTINUE_COMMAND = ConfigurationManager.AppSettings.Get("CONTINUE_COMMAND");
            MESSAGE_REQUEST = ConfigurationManager.AppSettings.Get("MESSAGE_REQUEST");

            ID = Convert.ToInt32(ConfigurationManager.AppSettings.Get("ID"));
            receiverNum = Convert.ToInt32(ConfigurationManager.AppSettings.Get("ReceiverNumber"));
            producerNum = Convert.ToInt32(ConfigurationManager.AppSettings.Get("ProducerNumber"));
            ratio = (float)producerNum / receiverNum;

            DB_LOGGING = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("DB_LOGGING"));
            //logging receiving socket
            if (DB_LOGGING == true)
            {
                String loggingDestinationPort = ConfigurationManager.AppSettings.Get("DBLoggingReceiver");
                loggerEmitter = zeromqContext.Socket(SocketType.PUSH);
                loggerEmitter.Bind(loggingDestinationPort);
                loggerThread = new Thread(new ThreadStart(this.LoggerThreadRun));
                loggerThread.Start();
            }

            switch (MESSAGING_TYPE)
            {
                case PIPELINE:
                    //  Socket to send messages on
                    if (addressSend != null)
                    {
                        sender = zeromqContext.Socket(SocketType.PUSH);
                        sender.Bind(addressSend);

                        // to receive continue and wait messages from message consumer
                        lbReceiver = zeromqContext.Socket(SocketType.SUB);
                        String lbReceiverAddress = ConfigurationManager.AppSettings.Get("ReceiveLoadBalancingAdress");
                        //load balancing messages can be received from multiple consumers
                        string[] addresses = lbReceiverAddress.Split(' ');
                        foreach (string address in addresses)
                        {
                            lbReceiver.Connect(address);
                            lbReceiver.Subscribe(ConfigurationManager.AppSettings.Get("RECEIVE_COMMAND_FILTER"), Encoding.UTF8);
                        }
                        //starts zeromq messaging thread
                        zerommqSendThread = new Thread(new ThreadStart(this.ZeromqSendThreadRun));
                        zerommqSendThread.Start();
                    }


                    if (addressReceive != null)
                    {
                        receiver = zeromqContext.Socket(SocketType.PULL);

                        string[] addresses = addressReceive.Split(' ');
                        foreach (string address in addresses)
                        {
                            receiver.Connect(address);
                        }
                        // to send continue and wait messages to message producers
                        String lbSenderAddress = ConfigurationManager.AppSettings.Get("SendLoadBalancingAddress");
                        lbSender = zeromqContext.Socket(SocketType.PUB);
                        lbSender.Bind(lbSenderAddress);

                        //starts zeromq receive messaging thread
                        zerommqReceiveThread = new Thread(new ThreadStart(this.ZeromqReceiveThreadRun));
                        zerommqReceiveThread.Start();
                    }

                    break;
                case REQ_REP:
                    if (addressSend != null)
                    {
                        sender = zeromqContext.Socket(SocketType.REP);
                        sender.Bind(addressSend);

                        //starts zeromq messaging thread
                        zerommqSendThread = new Thread(new ThreadStart(this.ZeromqSendThreadRun));
                        zerommqSendThread.Start();
                    }
                    break;
            }

            String finishPublishAddress = ConfigurationManager.AppSettings.Get("FinishPublish");
            String finishReceiveAddress = ConfigurationManager.AppSettings.Get("FinishReceive");
            if (finishPublishAddress != null)
            {
                finishSender = zeromqContext.Socket(SocketType.PUB);
                finishSender.Bind(finishPublishAddress);
            }
            else
            {
                finishReceiver = zeromqContext.Socket(SocketType.SUB);
                finishReceiver.Connect(finishReceiveAddress);
                finishReceiver.Subscribe(ConfigurationManager.AppSettings.Get("RECEIVE_COMMAND_FILTER"), Encoding.UTF8);
                finishThread = new Thread(new ThreadStart(this.FinishThreadRun));
                finishThread.Start();
            }

            //Thread for reading message files
            if (IGNORE_QUEUE_OVERFLOW == OFF)
            {
                //Enables file storage for handling data peaks
                fileThread = new Thread(new ThreadStart(this.FileThreadRun));
                //Reads previously written messages if they are not sent to WP4
                readOldMessageFiles();
                fileThread.Start();
            }
            //if (BROKER == ACTIVEMQ)
            //{
            //    try
            //    {
            //        //activemq opening connections
            //        Apache.NMS.ActiveMQ.ConnectionFactory factory = new Apache.NMS.ActiveMQ.ConnectionFactory(ConfigurationManager.AppSettings.Get("ACTIVEMQ"));
            //        activeMQconnection = factory.CreateConnection();
            //        Session session = activeMQconnection.CreateSession(AcknowledgementMode.AutoAcknowledge) as Session;
            //        IDestination bqueue = session.GetQueue(ConfigurationManager.AppSettings.Get("QueueName"));
            //        activemqSender = session.CreateProducer(bqueue);

            //        brokerThread = new Thread(new ThreadStart(this.ActiveMQBrokerThreadRun));
            //        brokerThread.Start();
            //    }
            //    catch (System.Exception e)
            //    {
            //        //     IGNORE_QUEUE_OVERFLOW = 1;
            //        BROKER = NONE;
            //        logger.Error(e);
            //    }
            //}
        }

        private void initLogger()
        {
            DOMConfigurator.Configure();
        }

        /*
         * method finishes messaging
         */
        public void stopMessaging()
        {
            finishSender.Send(FINISH_COMMAND, Encoding.UTF8, SendRecvOpt.NONE);
            messagingFinished = true;
        }

        public bool isMessagingFinished()
        {
            return messagingFinished;
        }

        public String getMessage()
        {
            try
            {
                if (BLOCKING_QUEUE || incomingMessageQueue.Count > 0)
                {
                    String message = incomingMessageQueue.Dequeue();
                    Thread.Sleep(1);
                    return message;
                }

            }
            catch (System.Exception e)
            {
                logger.Error(e.ToString());
            }
            return null;

        }

        /*
         * methods receives messages and puts into messaging queue
         */
        public void sendMessage(String message)
        {
            //sends message with zeromq
            if (outgoingMessageQueue.Count < Messenger.MAX_QUEUE_SIZE)
            {
                outgoingMessageQueue.Enqueue(message);
            }
            else if (IGNORE_QUEUE_OVERFLOW == OFF)
            {
                //sends message with the broker
                if (BROKER != NONE)
                {
                    try
                    {
                        if (brokerQueue.Count < Messenger.MAX_BROKER_QUEUE_SIZE)
                        {
                            brokerQueue.Enqueue(message);
                        }
                        else
                        {
                            logger.Debug("Message ignored");
                        }
                        //   producer.Send(message);
                    }
                    catch (System.Exception e)
                    {
                        //disables broker type messaging
                        IGNORE_QUEUE_OVERFLOW = ON;
                        BROKER = NONE;
                        logger.Error(e);
                    }
                }
                else
                {
                    writeOutgoingMessageToFile(message);

                    //keeps WP3
                    //lock (zeromqQueue)
                    //{
                    //    while (zeromqQueue.Count > Messenger.MAX_QUEUE_SIZE)
                    //    {
                    //        Monitor.Wait(zeromqQueue);
                    //    }
                    //    zeromqQueue.Enqueue(message);
                    //}
                }

            }
            //ignore the message
            else
            {
                logger.Debug("Message ignored");
            }

            Thread.Sleep(1);
        }

        /*
         * Activemq messaging thread
         */
        //public void ActiveMQBrokerThreadRun()
        //{
        //    while (true)
        //    {
        //        try
        //        {
        //            String value = (String)brokerQueue.Dequeue();
        //            activemqSender.Send(value);
        //            logger.Debug("Message is sent with activemq");
        //            Thread.Sleep(1);
        //            if (messagingFinished && brokerQueue.Count == 0)
        //            {
        //                activeMQconnection.Close();
        //                return;
        //            }
        //        }
        //        catch (System.Exception e)
        //        {
        //            //disables broker type messaging
        //            BROKER = NONE;
        //            logger.Error(e);
        //            brokerQueue.Clear();
        //            activeMQconnection.Close();
        //            return;
        //        }
        //    }
        //}

        /*
       * Finish messaging thread
       */
        public void FinishThreadRun()
        {
            byte[] command = finishReceiver.Recv(SendRecvOpt.NONE);
            messagingFinished = true;
        }


        /*
        * Logger thread
        */
        public void LoggerThreadRun()
        {
            while (true)
            {
                try
                {
                    String value = (String)loggerQueue.Dequeue();
                    loggerEmitter.Send(value, Encoding.UTF8, SendRecvOpt.NONE);
                    logger.Debug("Message is sent for logging");
                    Thread.Sleep(1);
                    if (messagingFinished && loggerQueue.Count == 0)
                    {
                        loggerEmitter.Send(FINISH_COMMAND, Encoding.UTF8, SendRecvOpt.NONE);
                        return;
                    }
                }
                catch (System.Exception e)
                {
                    //disables logging type messaging
                    DB_LOGGING = false;
                    loggerQueue.Clear();
                    logger.Error(e);
                    IGNORE_QUEUE_OVERFLOW = ON;
                    return;
                }
            }
        }

        /*
         * File storage thread
         */
        public void FileThreadRun()
        {
            while (!messagingFinished)
            {
                try
                {
                    //if there are no new messages in the queue, reads messages from the file storage
                    if (outgoingMessageQueue.Count < 5 && fileQueue.Count > 0)
                    {
                        String message = readOutGoingMessageFile();
                        outgoingMessageQueue.Enqueue(message);
                        Thread.Sleep(1);
                    }
                    else
                    {
                        Thread.Sleep(outgoingMessageQueue.Count * 10);
                    }

                    if (messagingFinished && fileQueue.Count == 0)
                    {
                        return;
                    }

                }
                catch (System.Exception e)
                {
                    logger.Error(e);
                    fileQueue.Clear();
                    return;
                }
            }
        }

        /*
         * Sends messages to consumers
         * 
         */
        public void ZeromqSendThreadRun()
        {
            logger.Debug("messaging type: " + MESSAGING_TYPE);
            double messageNum = 0;
            bool wait = false;
            switch (MESSAGING_TYPE)
            {
                case PIPELINE:
                    while (!messagingFinished)
                    {
                        try
                        {
                            //When a wait command is received, waits until the continue message
                            byte[] command = lbReceiver.Recv(SendRecvOpt.NOBLOCK);
                            if (command != null)
                            {
                                string commandString = System.Text.Encoding.UTF8.GetString(command);
                                if (commandString.StartsWith(WAIT_COMMAND))
                                {
                                    int clientNum = Convert.ToInt32(commandString.Split(' ')[1]);
                                    logger.Debug(ID + "wait message is received from : " + clientNum);
                                    lbSet.Add(clientNum);
                                }
                                else if (commandString.StartsWith(CONTINUE_COMMAND))
                                {
                                    int clientNum = Convert.ToInt32(commandString.Split(' ')[1]);
                                    lbSet.Remove(clientNum);
                                    logger.Debug(ID + "continue message is received from : " + clientNum);
                                }
                                if (lbSet.Count * ratio >= ID)
                                {
                                    wait = true;

                                }
                                else
                                {
                                    wait = false;
                                }



                            }
                            //Gets message from the queue added by the WP3
                            if (outgoingMessageQueue.Count > 0 && wait == false)
                            {
                                String value = (String)outgoingMessageQueue.Dequeue();
                                //Sends the message
                                sender.Send(value, Encoding.UTF8, SendRecvOpt.NONE);
                                logger.Debug("message is sent over network: " + messageNum++);
                                //if enabled, send a copy to DB_logging component
                                if (DB_LOGGING == true)
                                {
                                    loggerQueue.Enqueue(value);
                                }
                            }
                            else
                            {
                                logger.Debug(ID + "wait mode, size: " + lbSet.Count);
                                Thread.Sleep(10);
                            }
                            //Terminates the thread if finish message is retrived from WP3
                            //if (messagingFinished)
                            //{
                            //    sender.Send(FINISH_COMMAND, Encoding.UTF8, SendRecvOpt.NONE);
                            //    logger.Debug("Finish command is received");
                            //    return;
                            //}

                        }
                        catch (ThreadStateException e)
                        {
                            logger.Error(e);

                        }
                        catch (System.Exception e)
                        {
                            logger.Error(e);
                        }
                    }
                    while (outgoingMessageQueue.Count > 0)
                    {
                        writeOutgoingMessageToFile(outgoingMessageQueue.Dequeue());
                    }
                    break;
                case REQ_REP:
                    while (!messagingFinished)
                    {
                        try
                        {
                            //Waits request message from WP4
                            String message = sender.Recv(Encoding.UTF8);
                            while (!message.Equals(MESSAGE_REQUEST))
                            {
                                message = sender.Recv(Encoding.UTF8);
                            }
                            logger.Debug("Received request: {0}" + message);
                            // Sends WP3 message to WP4
                            if (outgoingMessageQueue.Count > 0)
                            {
                                String value = (String)outgoingMessageQueue.Dequeue();
                                sender.Send(value, Encoding.UTF8);
                                logger.Debug("message is sent over network: " + messageNum++);
                                //if enabled, send a copy to DB_logging component
                                if (DB_LOGGING == true)
                                {
                                    loggerQueue.Enqueue(value);
                                }
                            }
                        }
                        catch (ThreadStateException e)
                        {
                            logger.Error(e);

                        }
                        catch (System.Exception e)
                        {
                            logger.Error(e);
                            if (outgoingMessageQueue.Count > 0)
                            {
                                String value = (String)outgoingMessageQueue.Dequeue();
                                sender.Send(value, Encoding.UTF8);
                                logger.Debug("message is sent over network: " + messageNum++);
                                //if enabled, send a copy to DB_logging component
                                if (DB_LOGGING == true)
                                {
                                    loggerQueue.Enqueue(value);
                                }
                            }
                        }
                    }
                      while (outgoingMessageQueue.Count > 0)
                    {
                        writeOutgoingMessageToFile(outgoingMessageQueue.Dequeue());
                    }
                    break;
            }

        }

        /*
         * Messaging thread function handles communication between WP4
         * 
         */
        public void ZeromqReceiveThreadRun()
        {
            logger.Debug("receive messaging type: " + MESSAGING_TYPE);
            double messageNum = 0;
            bool wait = false;
            while (!messagingFinished)
            {
                try
                {
                    byte[] mes = receiver.Recv(SendRecvOpt.NOBLOCK);
                    while (mes != null && incomingMessageQueue.Count <= MAX_QUEUE_SIZE)
                    {
                        String message = System.Text.Encoding.UTF8.GetString(mes);

                        incomingMessageQueue.Enqueue(message);
                        logger.Debug("message received: " + messageNum++);
                        mes = receiver.Recv(SendRecvOpt.NOBLOCK);
                    }
                    // load balancing, sends wait and continue messages to WP3
                    if (incomingMessageQueue.Count > MAX_QUEUE_SIZE)
                    {
                        logger.Debug("Wait message is sent");
                        lbSender.Send(WAIT_COMMAND + " " + ID, Encoding.UTF8, SendRecvOpt.NOBLOCK);
                        Thread.Sleep(incomingMessageQueue.Count * 10);
                        wait = true;
                    }
                    else if (wait == true && incomingMessageQueue.Count <= MIN_QUEUE_SIZE)
                    {
                        logger.Debug("Continue message is sent");
                        lbSender.Send(CONTINUE_COMMAND + " " + ID, Encoding.UTF8, SendRecvOpt.NOBLOCK);
                        wait = false;
                    }
                    else if (wait == false && incomingMessageQueue.Count == 0)
                    {
                        lbSender.Send(CONTINUE_COMMAND + " " + ID, Encoding.UTF8, SendRecvOpt.NOBLOCK);
                        Thread.Sleep(10);
                    }
                    // to give context to the main thread
                    Thread.Sleep(1);


                }
                catch (System.Exception e)
                {
                    logger.Error(e);
                }
            }
            //after receiving the finish command incoming messages are written to files
            while (incomingMessageQueue.Count > 0)
            {
                writeIncomingMessageToFile(incomingMessageQueue.Dequeue());
            }
        }


        /*
         * Writes a message to a file
         */
        private void writeOutgoingMessageToFile(String content)
        {
            logger.Info(ID + "writeOutgoingMessageToFile: ");
            if (!fileQueue.Contains(fileNum.ToString()))
            {
                // create a writer and open the file
                TextWriter tw = new StreamWriter(outFileStorageAddress + "\\" + fileNum);
                // write a line of text to the file
                tw.Write(content);
                // close the stream
                tw.Close();

                fileQueue.Enqueue(fileNum.ToString());
                //reset counter
                if (fileNum >= maxFileStorageNum)
                {
                    fileNum = 1;
                }
                else
                {
                    fileNum++;
                }
                logger.Info(ID + "writesmessage: ");
            }
        }

        /*
         * Writes a message to a file
         */
        private void writeIncomingMessageToFile(String content)
        {
            // create a writer and open the file
            TextWriter tw = new StreamWriter(inFileStorageAddress + "\\" + inFileNum);
            // write a line of text to the file
            tw.Write(content);
            // close the stream
            tw.Close();
            inFileNum++;
        }

        /*
         * Reads previously written messages if they are not sent to consumers 
         */
        private void readOldMessageFiles()
        {
            // check folder exists if not then create
            if (!Directory.Exists(outFileStorageAddress))
            {
                Directory.CreateDirectory(outFileStorageAddress);
            }
            if (!Directory.Exists(inFileStorageAddress))
            {
                Directory.CreateDirectory(inFileStorageAddress);
            }

            //reads all file names and adds into the file queue
            string[] fileEntries = Directory.GetFiles(outFileStorageAddress);
            foreach (string fileName in fileEntries)
            {
                String file = fileName.Substring(outFileStorageAddress.Length + 1);
                fileQueue.Enqueue(file);
                int num = Convert.ToInt32(file);
                if (num >= fileNum)
                {
                    fileNum = num + 1;
                }

            }
            if (fileNum >= maxFileStorageNum)
            {
                fileNum = 1;
            }
            else
            {
                fileNum++;
            }

            //reads all file names and adds into the file queue
            string[] fileEntries2 = Directory.GetFiles(inFileStorageAddress);
            foreach (string fileName in fileEntries2)
            {
                String file = fileName.Substring(inFileStorageAddress.Length + 1);
                incomingMessageQueue.Enqueue(readIncomingMessageFile(file));
            }

        }
        /*
         * Reads a message file 
         */
        private String readIncomingMessageFile(String fileName)
        {
            StreamReader file = new StreamReader(inFileStorageAddress + "\\" + fileName);
            String content = file.ReadToEnd();
            file.Close();
            System.IO.File.Delete(inFileStorageAddress + "\\" + fileName);
            return content;
        }

        /*
         * Reads a message file 
         */
        private String readOutGoingMessageFile()
        {
            String fileName = (String)fileQueue.Peek();
            StreamReader file = new StreamReader(outFileStorageAddress + "\\" + fileName);
            String content = file.ReadToEnd();
            file.Close();
            System.IO.File.Delete(outFileStorageAddress + "\\" + fileName);
            fileQueue.Dequeue();
            return content;
        }


    }

    /*
     * Queue class 
     */
    class BlockingQueue<T> : IEnumerable<T>
    {
        private int _count = 0;

        public int Count
        {
            get { return _count; }
            set { _count = value; }
        }
        private Queue<T> _queue = new Queue<T>();

        public T Dequeue()
        {
            lock (this)
            {
                while (_count <= 0)
                    Monitor.Wait(this);
                _count--;

                Monitor.Pulse(this);
                return _queue.Dequeue();
            }
        }

        public T DequeueNoWait()
        {
            return _queue.Dequeue();

        }

        public void Enqueue(T data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            lock (this)
            {
                _queue.Enqueue(data);
                _count++;
                Monitor.Pulse(this);

            }
        }

        public T Peek()
        {
            return _queue.Peek();
        }

        public void Clear()
        {
            _queue.Clear();
        }

        public bool Contains(T data)
        {
            lock (this)
            {
                return _queue.Contains(data);

            }
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            while (true)
                yield return Dequeue();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}