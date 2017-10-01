﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeoGen.Core.Configurations;
using GeoGen.Core.Utilities;

namespace GeoGen.Analyzer
{
    /// <summary>
    /// A default implementation of <see cref="IGeometryHolder"/>. This
    /// class is not thread-safe.
    /// </summary>
    internal class GeometryHolder : IGeometryHolder
    {
        private const int NumberOfContainers = 5;

        private readonly IObjectsConstructor _constructor;

        private readonly IObjectsContainersFactory _factory;

        private readonly HashSet<int> _resolvedIds;

        private readonly List<IObjectsContainer> _containers;

        public GeometryHolder(IObjectsConstructor constructor, IObjectsContainersFactory factory)
        {
            _constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _resolvedIds = new HashSet<int>();
            _containers = new List<IObjectsContainer>();
        }

        public void Initialize(Configuration configuration)
        {
            var containers = Enumerable.Range(0, NumberOfContainers)
                    .Select(i => _factory.CreateContainer(configuration.LooseObjects));

            _containers.SetItems(containers);

            foreach (var constructedObject in configuration.ConstructedObjects)
            {
                if (!Register(constructedObject, out ConfigurationObject duplicate))
                    continue;

                if (duplicate == null)
                    throw new AnalyzerException("Unconstructible situation.");

                throw new AnalyzerException("Situation with duplicate objects");
            }
        }

        public bool Register(ConfigurationObject configurationObject, out ConfigurationObject duplicateVersion)
        {
            var id = configurationObject.Id ?? throw new AnalyzerException("Id must be set");

            if (_resolvedIds.Contains(id))
            {
                duplicateVersion = configurationObject;
                return false;
            }

            foreach (var container in _containers)
            {
                var geometricalObject = _constructor.Construct(configurationObject);

                // If the object is not constructible
                if (geometricalObject == null)
                {
                    duplicateVersion = null;
                    return false;
                }

                var result = container.Add(geometricalObject);

                if (geometricalObject != result)
                {
                    duplicateVersion = result.ConfigurationObject;
                    return false;
                }

                container.Add(geometricalObject);
            }

            _resolvedIds.Add(id);
            duplicateVersion = null;
            return true;
        }

        public void Remove(IEnumerable<ConfigurationObject> objects)
        {
            if (objects == null)
                throw new ArgumentNullException(nameof(objects));

            foreach (var configurationObject in objects)
            {
                var id = configurationObject.Id ?? throw new AnalyzerException("Id must be set");

                _resolvedIds.Remove(id);

                foreach (var container in _containers)
                {
                    container.Remove(id);
                }
            }
        }

        public IEnumerator<IObjectsContainer> GetEnumerator()
        {
            return _containers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}