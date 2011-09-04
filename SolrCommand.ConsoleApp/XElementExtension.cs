using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;

namespace Healthgrades.SolrSwap {

    /// <summary>
    /// XElement extention methods.
    /// </summary>
    public static class XElementExtension {

        /// <summary>
        /// Find the value of a Xml Element by the name of the element. 
        /// </summary>
        /// <param name="source">The element to search.</param>
        /// <param name="name">The name of the element.</param>
        /// <returns>The value of the element.</returns>
        public static String GetDesendantsSingleValue(this XElement source, XName name) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }

            if (name == null) {
                throw new ArgumentNullException("name");
            }
            try {
                XElement result = source.Descendants(name).SingleOrDefault();
                if (result == null) {
                    return string.Empty;
                }
                return result.Value;
            }
            catch (Exception ex) {
                throw new InvalidOperationException("Could not find element.", ex);
            }

        }

        /// <summary>
        /// Find the value of a Xml Element by the name of the element. 
        /// </summary>
        /// <param name="source">The element to search.</param>
        /// <param name="AttributeName">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <returns>The value of a </returns>
        public static String GetDescendantsByAttributeSingleValue(this XElement source, XName AttributeName, String value) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }

            if (AttributeName == null) {
                throw new ArgumentNullException("AttributeName");
            }

            if (value == null || String.IsNullOrEmpty(value.Trim())) {
                throw new ArgumentNullException("value");
            }
            try {
                var children = source.DescendantsAndSelf();
                XElement result = null;
                if (children != null) {
                    result = children.SingleOrDefault(ele => ele.Attribute(AttributeName) != null
                                                            && ele.Attribute(AttributeName).Value == value);
                }

                if (result == null) {
                    return string.Empty;
                }

                return result.Value;
            }
            catch (Exception ex) {
                throw new InvalidOperationException("Could not find element.", ex);
            } 
        }

        /// <summary>
        /// Find the value of a Xml Element by the name of the element. 
        /// </summary>
        /// <param name="source">The element to search.</param>
        /// <param name="AttributeName">The name of the attribute.</param>
        /// <param name="value">The value of the attribute.</param>
        /// <returns>The value of a </returns>
        public static XElement GetDescendantsByAttributeSingleOrDefault(this XElement source, XName AttributeName, String value) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }

            if (AttributeName == null) {
                throw new ArgumentNullException("AttributeName");
            }

            if (value == null || String.IsNullOrEmpty(value.Trim())) {
                throw new ArgumentNullException("value");
            }
            try {
                var children = source.DescendantsAndSelf();
                if (children != null) {
                    return children.SingleOrDefault(ele => ele.Attribute(AttributeName) != null
                                                            && ele.Attribute(AttributeName).Value == value);
                }

              
            }
            catch (Exception ex) {
                throw new InvalidOperationException("Could not find element.", ex);
            }

            return null;
        }
    }
}
