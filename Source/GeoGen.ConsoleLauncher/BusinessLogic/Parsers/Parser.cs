﻿using GeoGen.Core;
using GeoGen.Generator;
using GeoGen.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GeoGen.ConsoleLauncher
{
    /// <summary>
    /// The default implementation of <see cref="IParser"/>.
    /// </summary>
    public class Parser : IParser
    {
        /// <summary>
        /// Parses a given content to a generator input.
        /// </summary>
        /// <param name="content">The content of an input file.</param>
        /// <returns>The parsed generator input.</returns>
        public GeneratorInput ParseInput(string content)
        {
            // Get the lines 
            var lines = content.Split('\n')
                // Trimmed
                .Select(s => s.Trim())
                // That are not comments
                .Where(line => !line.StartsWith('#') && !string.IsNullOrEmpty(line))
                // As a list
                .ToList();

            #region Parsing iterations

            // Find the iteration number. First find the iteration line
            var iterationsNumber = lines.Select(line => Regex.Match(line, "^Iterations:(.+)$"))
                // Take the first where there is a match
                .FirstOrDefault(match => match.Success)
                // Take the number from the group
                ?.Groups[1].Value.Trim()
                // If there is no, make aware
                ?? throw new ParserException("No line specifying the number of iterations in the form 'Iterations: number'");

            // Prepare the number of iterations
            var numberOfIterations = default(int);

            try
            {
                // Try to parse 
                numberOfIterations = int.Parse(iterationsNumber);

                // Make sure it's a correct value
                if (numberOfIterations < 0)
                    throw new ParserException($"The number of iterations cannot be negative, the found value is {numberOfIterations}.");
            }
            catch (Exception e) when (e is FormatException || e is OverflowException)
            {
                // Make sure the user is aware
                throw new ParserException($"Cannot parse the number of iterations: '{iterationsNumber}'");
            }

            #endregion

            #region Parsing constructions

            // Get the index of the starting line
            var constructionLineIndex = lines.IndexOf("Constructions:");

            // Make sure there is some
            if (constructionLineIndex == -1)
                throw new ParserException("No line starting the constructions section in the form: 'Constructions:'");

            // Get the index of the starting line
            var configurationLineIndex = lines.IndexOf("Initial configuration:");

            // Make sure there is some
            if (configurationLineIndex == -1)
                throw new ParserException("No line starting the initial configuration section in the form: 'Initial configuration:'");

            // Make sure the construction line is before the configuration one
            if (constructionLineIndex > configurationLineIndex)
                throw new ParserException("Constructions should be specified before the initial configuration.");

            // Get the constructions...Get the lines between the found indices
            var constructions = lines.Skip(constructionLineIndex + 1).Take(configurationLineIndex - constructionLineIndex - 1)
                // Each line defines a construction
                .Select(ParseConstruction)
                // Enumerate to a list
                .ToList();

            #endregion

            // Parse configuration
            var configuration = ParseConfiguration(lines.Skip(configurationLineIndex + 1).ToList()).configuration;

            // Return the final input
            return new GeneratorInput
            {
                InitialConfiguration = configuration,
                Constructions = constructions,
                NumberOfIterations = numberOfIterations,
            };
        }

        /// <summary>
        /// Parses a given content to a list of theorems.
        /// </summary>
        /// <param name="content">The content of a file containing template theorems.</param>
        /// <returns>The parsed theorems.</returns>
        public List<Theorem> ParseTheorems(string content)
        {
            // Get the lines 
            var lines = content.Split('\n')
                // Trimmed
                .Select(s => s.Trim())
                // That are not comments
                .Where(line => !line.StartsWith('#') && !string.IsNullOrEmpty(line))
                // As a list
                .ToList();

            #region Parsing configuration

            // Get the index of the configuration starting line
            var configurationLineIndex = lines.IndexOf("Configuration:");

            // Make sure there is some
            if (configurationLineIndex == -1)
                throw new ParserException("No line starting the configuration section in the form: 'Configuration:'");

            // Get the index of the theorem starting line
            var theoremsLineIndex = lines.IndexOf("Theorems:");

            // Make sure there is some
            if (theoremsLineIndex == -1)
                throw new ParserException("No line starting the theorems section in the form: 'Theorems:'");

            // Get the configuration lines
            var configurationLines = lines.Skip(configurationLineIndex + 1).Take(theoremsLineIndex - configurationLineIndex - 1).ToList();

            // Parse the configuration
            var (configuration, namesToObjects) = ParseConfiguration(configurationLines);

            #endregion

            // Otherwise parse each theorem line
            return lines.Skip(theoremsLineIndex + 1).Select(line => ParseTheorem(line, namesToObjects, configuration)).ToList();
        }

        /// <summary>
        /// Parses a construction from its name.
        /// </summary>
        /// <param name="constructionName">The name of the construction.</param>
        /// <returns>The parsed construction.</returns>
        private static Construction ParseConstruction(string constructionName)
        {
            // Local helper that returns a string informing about available constructions
            static string AvailableConstructions() => $"Available: \n\n" +
                // Get all constructions, prepend -, sort, and make every on a single line
                $"{Constructions.GetAllConstructions().Select(c => $" - {c}").Ordered().ToJoinedString("\n")}.\n";

            // Try to find if it is a predefined one
            if (Enum.TryParse<PredefinedConstructionType>(constructionName, out var type))
                return Constructions.GetPredefinedconstruction(type);

            // Try to find if the construction doesn't start with RandomPointOn
            var match = Regex.Match(constructionName, "^RandomPointOn(.*)$");

            // If yes
            if (match.Success)
            {
                // Get the rest of the name
                var restOfName = match.Groups[1].Value;

                // Try to parse it 
                // If it worked out, wrap the result in the RandomPointOn construction
                if (Enum.TryParse<PredefinedConstructionType>(constructionName, out var _type))
                    return RandomPointOnConstruction.RandomPointOn(_type);

                // Try to get the composed one
                var composedConstruction = Constructions.GetComposedConstruction(restOfName);

                // If it worked out, wrap the result in the RandomPointOn construction 
                if (composedConstruction != null)
                    return RandomPointOnConstruction.RandomPointOn(composedConstruction);

                // Otherwise we won't allow a composed construction with a name like this
                throw new ParserException("If the name of a construction starts with RandomPointOn, " +
                    $"then the rest must be a correct construction name. {AvailableConstructions()}");
            }

            // If not, it should be a composed one. 
            // Right now they are all stored in a static class. Try to find it there...
            return Constructions.GetComposedConstruction(constructionName)
                // if it's not found, make the user aware
                ?? throw new ParserException($"Unknown construction '{constructionName}'. {AvailableConstructions()}");
        }

        /// <summary>
        /// Parses a configuration from configuration lines.
        /// </summary>
        /// <param name="configurationLines">The lines with the configuration's definition.</param>
        /// <returns>The tuple consisting of the parsed configuration and the dictionary mapping declared object names to their real objects.</returns>
        private (Configuration configuration, Dictionary<string, ConfigurationObject> namesToObjects) ParseConfiguration(List<string> configurationLines)
        {
            // Make sure there is at least one line
            if (configurationLines.Count == 0)
                throw new ParserException("The configuration has no definition.");

            // Prepare a dictionary between names and parsed objects
            var namesToObjects = new Dictionary<string, ConfigurationObject>();

            #region Parse loose objects

            // The loose objects should be specified in the first line
            var looseObjectsMatch = Regex.Match(configurationLines[0], "^(.+):(.+)$");

            // Make sure there is a match
            if (!looseObjectsMatch.Success)
                throw new ParserException("The first line of the configuration specification should be in the form 'Layout: objects', where objects are separated by commas.");

            // Get the layout string
            var layoutString = looseObjectsMatch.Groups[1].Value.Trim();

            // Try to parse the layout
            if (!Enum.TryParse(looseObjectsMatch.Groups[1].Value, out LooseObjectsLayout layout))
                throw new ParserException($"Cannot parse the loose objects layout '{looseObjectsMatch.Groups[1].Value}'");

            // Get the loose objects names, separated by commas
            var looseObjectsNames = looseObjectsMatch.Groups[2].Value.Split(",").Select(s => s.Trim()).ToList();

            // Find the needed types of loose objects for the layout
            var neededObjectTypes = layout.ObjectTypes();

            // Make sure the number of found objects matches the number of needed ones
            if (neededObjectTypes.Count != looseObjectsNames.Count)
                throw new ParserException("The number of parsed loose objects doesn't match the layout.");

            // Create real loose objects from names
            var looseObjects = looseObjectsNames.Select((name, index) =>
            {
                // Make sure the name hasn't been used
                if (namesToObjects.ContainsKey(name))
                    throw new ParserException($"Error while parsing loose objects. The object with the name '{name}' has been already at least twice.");

                // Make sure the name is correct
                if (!name.All(char.IsLetterOrDigit))
                    throw new ParserException($"Error while parsing loose objects. Name of an object can contain only letters and digits, this one is '{name}'");

                // Create a loose object of the needed type
                var looseObject = new LooseConfigurationObject(neededObjectTypes[index]);

                // Associate it with the name
                namesToObjects.Add(name, looseObject);

                // Return it
                return looseObject;
            })
            // Enumerate
            .ToList();

            // Create the holder of loose objects
            var holder = new LooseObjectsHolder(looseObjects, layout);

            #endregion

            #region Parse constructed objects

            // Skip the first line and parse the rest
            var constructedObjects = configurationLines.Skip(1).Select(line =>
            {
                // Let us match the name and the definition first
                var nameDefinitionMatch = Regex.Match(line, "^(.+)=(.+)$");

                // Make sure there's a match...
                if (!nameDefinitionMatch.Success)
                    throw new ParserException($"Error while parsing '{line}'. The line should be in the form 'name = definition'.");

                // Get the name
                var newObjectName = nameDefinitionMatch.Groups[1].Value.Trim();

                // Make sure the name is correct
                if (!newObjectName.All(char.IsLetterOrDigit))
                    throw new ParserException($"Error while parsing '{line}'. Name of an object can contain only letters and digits, this one is '{newObjectName}'");

                // Make sure the name hasn't been used
                if (namesToObjects.ContainsKey(newObjectName))
                    throw new ParserException($"Error while parsing '{line}'. The object with the name '{newObjectName}' has been already declared at least twice.");

                // Match the construction and input objects from the definition
                var definitionMatch = Regex.Match(nameDefinitionMatch.Groups[2].Value.Trim(), "^(.+)\\((.*)\\)$");

                // Make sure there's a match...
                if (!definitionMatch.Success)
                    throw new ParserException($"Error while parsing '{line}'. The definition should be in the form 'ConstructioName(objects)'.");

                // Prepare the construction
                var construction = default(Construction);

                try
                {
                    // Try to get it 
                    construction = ParseConstruction(definitionMatch.Groups[1].Value);
                }
                catch (ParserException e)
                {
                    // Re-throw the exception with the line info
                    throw new ParserException($"Error while parsing '{line}'. {e.Message}");
                }

                // Get the passed objects
                var passedObjects = definitionMatch.Groups[2].Value.Split(",").Select(s => s.Trim()).Where(s => !s.IsNullOrEmpty()).Select(name =>
                {
                    // Make sure the name has been declared before
                    if (!namesToObjects.ContainsKey(name))
                        throw new ParserException($"Error while parsing '{line}'. Undeclared object '{name}'");

                    // Return the corresponding object
                    return namesToObjects[name];
                })
                // Enumerate
                .ToArray();

                try
                {
                    // Try to create a constructed object
                    var constructedObject = new ConstructedConfigurationObject(construction, passedObjects);

                    // Associate it
                    namesToObjects.Add(newObjectName, constructedObject);

                    // Return it
                    return constructedObject;
                }
                catch (GeoGenException e)
                {
                    // Make sure the user is aware of the problem
                    throw new ParserException(e.Message);
                }
            })
            // Enumerate
            .ToList();

            #endregion

            // Finally create the result
            return (new Configuration(holder, constructedObjects), namesToObjects);
        }

        /// <summary>
        /// Parses a theorem from a theorem line.
        /// </summary>
        /// <param name="theoremLine">The line with the theorem's description.</param>
        /// <param name="namesToObjects">The dictionary mapping declared object names to their real objects.</param>
        /// <param name="configuration">The configuration where the theorem holds.</param>
        /// <returns>The parsed theorem.</returns>
        private Theorem ParseTheorem(string theoremLine, Dictionary<string, ConfigurationObject> namesToObjects, Configuration configuration)
        {
            #region Parsing type

            // Match the type
            var typeDefinitionMatch = Regex.Match(theoremLine, "^(.+):(.+)$");

            // Make sure there is a match...
            if (!typeDefinitionMatch.Success)
                throw new ParserException($"Error while parsing '{theoremLine}'. The theorem should be in the form 'name:definition'");

            // Parse the type
            if (!Enum.TryParse<TheoremType>(typeDefinitionMatch.Groups[1].Value, out var theoremType))
                throw new ParserException($"Error while parsing '{theoremLine}'. Unknown theorem type '{typeDefinitionMatch.Groups[1].Value}'");

            #endregion

            #region Parsing objects

            // Get the definition string
            var definition = typeDefinitionMatch.Groups[2].Value.Trim();

            // In order to find theorem objects, we will look for those commas
            // that are not in any unclosed brackets of any type. For simplicity,
            // we will replace them with semicolons and then split the split by them
            // (speed does not matter here very much)

            // Copy the definition so we can edit it
            var definitionCopy = new StringBuilder(definition);

            // Prepare the number of unclosed brackets
            var unclosedBrackets = 0;

            // Go through the characters
            definition.ForEach((c, index) =>
            {
                // If we have an open bracket, count it in
                if (c == '(' || c == '[')
                    unclosedBrackets++;

                // If we have a closed one, count it out
                else if (c == ')' || c == ']')
                    unclosedBrackets--;

                // If we have a comma and there are no unclosed brackets,
                // replace it with a semicolon
                else if (c == ',' && unclosedBrackets == 0)
                    definitionCopy[index] = ';';
            });

            // Split the copied string by created semicolons to get particular objects
            var theoremObjectsStrings = definitionCopy.ToString().Split(';').Select(s => s.Trim());

            // Parse the objects
            var theoremObjects = theoremObjectsStrings.Select(theoremObject => ParseTheoremObject(theoremObject, namesToObjects)).ToList();

            #endregion

            // Finally construct the theorem
            return Theorem.DeriveFromFlattenedObjects(configuration, theoremType, theoremObjects);
        }

        /// <summary>
        /// Parses theorem object from a given string.
        /// </summary>
        /// <param name="objectString">The string containing the object's definition.</param>
        /// <param name="namesToObjects">The dictionary mapping declared object names to their real objects.</param>
        /// <returns>The parsed theorem object.</returns>
        private static TheoremObject ParseTheoremObject(string objectString, Dictionary<string, ConfigurationObject> namesToObjects)
        {
            // Try to match a line defined implicitly
            var implicitLineMatch = Regex.Match(objectString, "^\\[(.+)\\]$");

            // Try to match a circle defined implicitly
            var implicitCircleMatch = Regex.Match(objectString, "^\\((.+)\\)$");

            // Try to match a line with both explicit and implicit definitions
            var explicitImplicitLineMatch = Regex.Match(objectString, "^(.+)\\[(.+)\\]$");

            // Try to match a circle with both explicit and implicit definitions
            var explicitImplicitCircleMatch = Regex.Match(objectString, "^(.+)\\((.+)\\)$");

            // Get the implicit points names. 
            var pointNames = implicitLineMatch.Success ? implicitLineMatch.Groups[1].Value :
                             implicitCircleMatch.Success ? implicitCircleMatch.Groups[1].Value :
                             explicitImplicitLineMatch.Success ? explicitImplicitLineMatch.Groups[2].Value :
                             explicitImplicitCircleMatch.Success ? explicitImplicitCircleMatch.Groups[2].Value
                             // There might not be defined at all
                             : null;

            // Get the explicit object name. 
            var objectName = explicitImplicitLineMatch.Success ? explicitImplicitLineMatch.Groups[1].Value :
                             explicitImplicitCircleMatch.Success ? explicitImplicitCircleMatch.Groups[1].Value :
                             implicitLineMatch.Success ? null :
                             implicitCircleMatch.Success ? null :
                             // If there is no match, we assume the whole string is the name
                             objectString;


            // Parse the points, if there are any
            var theoremPoints = pointNames?.Split(",").Select(s => s.Trim()).Select(pointName =>
            {
                // Make sure such a point exist 
                if (!namesToObjects.ContainsKey(pointName))
                    throw new ParserException($"Error while parsing '{objectString}'. Unknown object '{pointName}'.");

                // Get the corresponding object
                var pointObject = namesToObjects[pointName];

                // Make sure it is a point
                if (pointObject.ObjectType != ConfigurationObjectType.Point)
                    throw new ParserException($"Error while parsing '{objectString}'. Object {pointName} is not a point.");

                // Return it
                return pointObject;
            })
            // Enumerate
            .ToArray();

            // Make sure the points are mutually distinct
            if (theoremPoints != null && theoremPoints.Distinct().Count() != theoremPoints.Length)
                throw new ParserException($"Error while parsing '{objectString}'. Points are not mutually distinct.");

            // Prepare a configuration object
            var configurationObject = default(ConfigurationObject);

            // Find the type of the object we're currently parsing
            var type = default(ConfigurationObjectType);

            // If the object is specified...
            if (objectName != null)
            {
                // Make sure the name has been defined
                if (!namesToObjects.ContainsKey(objectName))
                    throw new ParserException($"Error while parsing '{objectString}'. Unknown object '{objectName}'.");

                // Set the corresponding object
                configurationObject = namesToObjects[objectName];

                // Set the type
                type = configurationObject.ObjectType;

                // Make sure the explicit circle is declared correctly
                if (explicitImplicitCircleMatch.Success && configurationObject.ObjectType == ConfigurationObjectType.Line)
                    throw new ParserException($"Error while parsing '{objectString}'. Line {objectName} should have had its points specified in [].");

                // Make sure the explicit circle is declared correctly
                if (explicitImplicitLineMatch.Success && configurationObject.ObjectType == ConfigurationObjectType.Circle)
                    throw new ParserException($"Error while parsing '{objectString}'. Circle {objectName} should have had its points specified in ().");

                // Make sure point does not have points specified
                if ((explicitImplicitCircleMatch.Success || explicitImplicitLineMatch.Success) && configurationObject.ObjectType == ConfigurationObjectType.Point)
                    throw new ParserException($"Error while parsing '{objectString}'. Point names cannot be followed by brackets.");
            }
            // If the object isn't specified...
            else
            {
                // Then we must have a line or circle defined implicitly by points
                // Figure out the type
                type = implicitLineMatch.Success ? ConfigurationObjectType.Line : ConfigurationObjectType.Circle;

                // Find the number of needed points
                var numberOfNeededPoints = type == ConfigurationObjectType.Line ? 2 : 3;

                // We need to make sure there are enough points
                if (theoremPoints.Length < numberOfNeededPoints)
                    throw new ParserException($"Error while parsing '{objectString}'. {type} needs at least {numberOfNeededPoints} points.");
            }

            // Switch based on the figured type which constructor we'll use
            return type switch
            {
                // Point case
                ConfigurationObjectType.Point => new PointTheoremObject(configurationObject) as TheoremObject,

                // Line case
                ConfigurationObjectType.Line => new LineTheoremObject(configurationObject, theoremPoints),

                // Circle case
                ConfigurationObjectType.Circle => new CircleTheoremObject(configurationObject, theoremPoints),

                // Default case
                _ => throw new GeoGenException($"Unhandled type of configuration object: {type}"),
            };
        }
    }
}