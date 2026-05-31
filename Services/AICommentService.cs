using System;
using System.Text;
using System.Threading.Tasks;

namespace SmartCityPulse.Services
{
    public class AICommentService
    {
        public Task<string> GenerateCommentAsync(string title, string description, string severity, string status, string department)
        {
            var comment = new StringBuilder();
            var now = DateTime.UtcNow.ToString("HH:mm");

            comment.Append($"[{now}] ");

            // ========== STATUS: OPEN ==========
            if (status == "Open")
            {
                comment.Append($"⚠️ Incident reported: \"{title}\". ");

                if (severity == "Critical")
                {
                    comment.Append($"🚨 CRITICAL EMERGENCY: Immediate response team dispatched to {department}. Highest priority assigned. ");
                }
                else if (severity == "High")
                {
                    comment.Append($"🔴 HIGH PRIORITY: Response team en route to location. Emergency services notified. ");
                }
                else if (severity == "Medium")
                {
                    comment.Append($"🟡 MEDIUM PRIORITY: Investigation initiated. Field team being deployed. ");
                }
                else
                {
                    comment.Append($"🟢 ROUTINE RESPONSE: Incident logged in system. Team will be assigned shortly. ");
                }

                if (department.Contains("Police"))
                {
                    comment.Append($"Police patrol dispatched to scene. FIR registration in process. ");
                }
                else if (department.Contains("Fire"))
                {
                    comment.Append($"Fire tenders dispatched. Evacuation protocol activated if needed. ");
                }
                else if (department.Contains("Rescue"))
                {
                    comment.Append($"Ambulance dispatched. Medical team on standby. ");
                }

                comment.Append($"Residents advised to stay clear of the area. Next update in 15 minutes.");
            }

            // ========== STATUS: IN PROGRESS ==========
            else if (status == "In Progress")
            {
                comment.Append($"🔄 UPDATE: Response team actively working on \"{title}\". ");

                if (department.Contains("Police"))
                {
                    comment.Append($"Police investigation in progress. Evidence collection underway. Witness statements being recorded. ");
                }
                else if (department.Contains("Fire"))
                {
                    if (severity == "Critical")
                    {
                        comment.Append($"Firefighting operations active at scene. Multiple units deployed. Fire being contained. ");
                    }
                    else
                    {
                        comment.Append($"Firefighters on scene. Cooling operations in progress. Situation under control. ");
                    }
                }
                else if (department.Contains("Rescue"))
                {
                    comment.Append($"Rescue team on site. First aid being administered. Patient being stabilized for transport. ");
                }
                else
                {
                    comment.Append($"Field team assessing situation. Necessary actions being taken. ");
                }

                comment.Append($"Estimated resolution time: 30-45 minutes. Further updates to follow.");
            }

            // ========== STATUS: RESOLVED ==========
            else if (status == "Resolved")
            {
                comment.Append($"✅ RESOLVED: Incident \"{title}\" successfully closed. ");

                if (department.Contains("Police"))
                {
                    comment.Append($"Case investigation complete. FIR filed. Area declared safe. ");
                }
                else if (department.Contains("Fire"))
                {
                    comment.Append($"Fire completely extinguished. Area declared safe. No further risk identified. ");
                }
                else if (department.Contains("Rescue"))
                {
                    comment.Append($"Patient treated and transported to hospital. Condition stable. Family notified. ");
                }

                comment.Append($"Thank you for your cooperation. Report filed for records.");
            }

            // ========== DEFAULT ==========
            else
            {
                comment.Append($"📋 Incident \"{title}\" is under review by {department} department. ");
                comment.Append($"Officials are assessing the situation. Further updates will be provided as information becomes available.");
            }

            return Task.FromResult(comment.ToString());
        }
    }
}