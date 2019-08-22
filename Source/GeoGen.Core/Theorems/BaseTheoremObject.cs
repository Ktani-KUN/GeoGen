﻿namespace GeoGen.Core
{
    /// <summary>
    /// Represents a <see cref="TheoremObject"/> that can be defined by a <see cref="Core.ConfigurationObject"/>.
    /// </summary>
    public abstract class BaseTheoremObject : TheoremObject
    {
        #region Public properties

        /// <summary>
        /// Gets the configuration object corresponding to this object. 
        /// </summary>
        public ConfigurationObject ConfigurationObject { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TheoremObject"/> class.
        /// </summary>
        /// <param name="configurationObject">The configuration object corresponding to this object.</param>
        protected BaseTheoremObject(ConfigurationObject configurationObject = null)
        {
            ConfigurationObject = configurationObject;
        }

        #endregion

        #region Public abstract methods implementation

        /// <summary>
        /// Determines if a given theorem object is equivalent to this one,
        /// i.e. if they represent the same object of a configuration.
        /// </summary>
        /// <param name="otherObject">The theorem object.</param>
        /// <returns>true if they are equivalent; false otherwise.</returns>
        public override bool IsEquivalentTo(TheoremObject otherObject)
        {
            // Either there instance are the same
            return this == otherObject ||
                // Or we have another base object
                otherObject is BaseTheoremObject baseObject &&
                // And both this and the other object have their configuration objects set
                ConfigurationObject != null && baseObject.ConfigurationObject != null &&
                // And these objects are the same
                ConfigurationObject.Equals(baseObject.ConfigurationObject);
        }

        #endregion
    }
}
