using System;
using System.Xml;
using UnityEngine;

/*
 * Copyright (c) 2017 Sloan Kelly 
 * [Licensed under the MIT License]
 *
 * See LICENSE file for full license text.
 */

public class BaseOsm
{
    /// <summary>
    /// Get an attribute's value from the collection using the given 'attrName'. 
    /// </summary>
    /// <typeparam name="T">Data type</typeparam>
    /// <param name="attrName">Name of the attribute</param>
    /// <param name="attributes">Node's attribute collection</param>
    /// <returns>The value of the attribute converted to the required type</returns>
    protected T GetAttribute<T>(string attrName, XmlAttributeCollection attributes)
    {
        XmlAttribute attr = attributes[attrName];
        if (attr == null)
        {
            Debug.LogWarning($"Missing XML attribute: {attrName}");
            return default(T);
        }

        string strValue = attr.Value;
        T instance = default(T);

        try
        {
            instance = (T)Convert.ChangeType(strValue, typeof(T));
        }
        catch (Exception e)
        {
            // Fix for height values ending with 'm' which seems to be something new.
            if (strValue.EndsWith("m"))
            {
                var idx = strValue.IndexOf(' ');
                if (idx >= 0)
                {
                    var tmp = strValue.Substring(0, idx);
                    instance = (T)Convert.ChangeType(tmp, typeof(T));
                }
            }
            else
            {
                Debug.Log(e.ToString() + "\r\nname=" + attrName + ", value=" + strValue);
            }
        }

        return instance;
    }
}
