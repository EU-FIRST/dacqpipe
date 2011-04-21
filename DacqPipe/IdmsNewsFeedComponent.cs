/*==========================================================================;
 *
 *  This file is part of LATINO. See http://latino.sf.net
 *
 *  File:    IdmsNewsFeedComponent.cs
 *  Desc:    News feed component based on IDMS API
 *  Created: Apr-2010
 *
 *  Authors: Miha Grcar
 *
 ***************************************************************************/

using Latino.Workflows;

namespace Dacq
{
    public class IdmsNewsFeedComponent : StreamDataProducer
    {
        public IdmsNewsFeedComponent(string loggerBaseName) : base(loggerBaseName)
        {
        }

        public override void Start()
        {
        }

        public override void Stop()
        {
        }

        public override bool IsRunning
        {
            get { return false; }
        }

        // *** IDisposable interface implementation ***

        public override void Dispose()
        {
            mLogger.Debug("Dispose", "Disposing ...");
            Stop();
            mLogger.Debug("Dispose", "Disposed.");
        }
    }
}
