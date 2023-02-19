# dotBot

An VisualStudio2022 project that is a web application that implements the functions of the chat bot VKontakte

Frameworks used in the project:
Entity Framework
vknet
netonsoft.json

EntityFramework and T-SQL queries are used to access the database (MsSqlServer)
 
Communication with the Vk API is done using the VkNet framework

Implemented:
•Authorization by community token
• Receiving an event from Vk CallBack
•Processing the response received from the VK server (Checking for the presence of triggers, checking for the presence of entityes in the database, creating in case of absence)
•Performing the requested functionality
• Sending a request to Vkontakte

The project is a game chatbot that implements the mechanics of RPG games (character leveling, killing, earning currency)

Chat management functionality is also implemented (kick, ban (requires improvement), administrator appointment)

Planned:
•Implementation of a game store in which the user will be able to sell the currency earned in the process
•Selection from the database of users by the amount of experience or money to create an overall rating
•Fetching conversations from the database to create a list of conversations on the site
•Authorization on the site using Vkontakte
