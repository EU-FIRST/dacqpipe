/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    ZeroMqEmitterComponent.cs
 *  Desc:    ZeroMQ emitter component
 *  Created: Sep-2011
 *
 *  Author:  Miha Grcar
 *
 ***************************************************************************/

using System.Xml;
using System.IO;
using Latino.Workflows.TextMining;
using Messaging;
using System.Text;

namespace Latino.Workflows.Persistance
{
    /* .-----------------------------------------------------------------------
       |
       |  Class ZeroMqEmitter
       |
       '-----------------------------------------------------------------------
    */
    public class ZeroMqEmitterComponent : StreamDataConsumer
    {
        private Messenger mMessenger 
            = new Messenger();

        public ZeroMqEmitterComponent() : base(typeof(ZeroMqEmitterComponent))
        {
        }

        protected override void ConsumeData(IDataProducer sender, object data)
        {
            Utils.ThrowException(!(data is DocumentCorpus) ? new ArgumentTypeException("data") : null);
            StringWriter stringWriter;
            XmlWriterSettings xmlSettings = new XmlWriterSettings();
            xmlSettings.Indent = true;
            xmlSettings.NewLineOnAttributes = true;
            xmlSettings.CheckCharacters = false;
            XmlWriter writer = XmlWriter.Create(stringWriter = new StringWriter(), xmlSettings);
            ((DocumentCorpus)data).WriteXml(writer, /*writeTopElement=*/true);
            writer.Close();
            // send message
            mMessenger.sendMessage(stringWriter.ToString());
        }

        // *** IDisposable interface implementation ***

        public new void Dispose()
        {
            try
            {
                mMessenger.stopMessaging();
            }
            catch
            {
            }
        }
    }
}