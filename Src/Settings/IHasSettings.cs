using System;

namespace RT.Util.Settings
{
    /// <summary>
    /// This interface is implemented by classes which expose settings that need to
    /// be persisted across program restarts. The main purpose of the interface is
    /// to document the correct way of implementing settings and mark classes which
    /// promise to follow the rules laid out in this documentation.
    /// </summary>
    /// 
    /// <remarks>
    /// A class that wants its settings exposed through this mechanism should
    /// do the following:
    /// <list type="bullet">
    /// <item>Declare a nested type called "Settings" defining all the fields which need
    /// to be saved.</item>
    /// <item>Declare a *private* variable called "_settings" of the type "Settings".</item>
    /// <item>Implement <see cref="IHasSettings"/> and follow the requirements documented
    /// in the XML comments for each interface member.</item>
    /// </list>
    /// <para>
    /// The interface member comments provide some boilerplate code to be used in the
    /// implementations.
    /// </para>
    /// <para>
    /// A class wanting to derive from a class which implements <see cref="IHasSettings"/>
    /// doesn't need to do anything special unless it introduces some of its own settings
    /// to be saved. In this case, the derived class should:
    /// </para>
    /// <list type="bullet">
    /// <item>Declare a nested type called "Settings" which *derives* from the "Settings"
    /// type of its base class.</item>
    /// <item>Declare the private "_settings" variable of the new "Settings" type.</item>
    /// <item>Override all members of the <see cref="IHasSettings"/> interface, providing
    /// the code to load/save settings specific to the derived type, and calling the base
    /// class's implementations to load/save base class's settings.</item>
    /// </list>
    /// </remarks>
    public interface IHasSettings
    {
        /// <summary>
        /// Called by the startup routines to specify the instance of the class from
        /// which the implementer should load it settings.
        /// </summary>
        /// 
        /// <remarks>
        /// The implementer must:
        /// <list type="bullet">
        /// <item>Declare the method as virtual unless the class is marked "sealed".</item>
        /// <item>Throw an <see cref="ArgumentException"/> if passed null or an instance
        /// of the wrong type.</item>
        /// <item>Load and apply the settings from the specified instance.</item>
        /// <item>Store the instance for later use when <see cref="SaveSettings"/> is
        /// called.</item>
        /// <item>Not throw any other exceptions otherwise - instead use default values
        /// for settings that can't be read correctly.</item>
        /// </list>
        /// 
        /// <code>
        /// // Boilerplate code
        /// if (!(settings is Settings)) throw new ArgumentException("Argument is null or of the wrong type.");
        /// else _settings = (Settings) settings;
        /// // End boilerplate
        /// </code>
        /// </remarks>
        void SetSettings(object settings);

        /// <summary>
        /// Requests the class to store its settings in the settings class instance
        /// provided in the previous call to <see cref="SetSettings"/>.
        /// </summary>
        /// 
        /// <remarks>
        /// The implementer must:
        /// <list type="bullet">
        /// <item>Store all of its current settings in the settings class instance.</item>
        /// <item>If <see cref="SetSettings"/> has never been called before, must throw
        /// an <see cref="InvalidOperationException"/>.</item>
        /// </list>
        /// 
        /// <code>
        /// // Boilerplate code
        /// if (_settings == null) throw new InvalidOperationException("SaveSettings called before SetSettings.");
        /// // End boilerplate
        /// </code>
        /// </remarks>
        void SaveSettings();
    }
}
