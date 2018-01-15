﻿using GTANetworkAPI;
using WiredPlayers.database;
using WiredPlayers.globals;
using WiredPlayers.model;
using System.Collections.Generic;
using System;

namespace WiredPlayers.bank
{
    public class Bank : Script
    {
        public Bank()
        {
            Event.OnClientEventTrigger += OnClientEvent;
        }

        private void OnClientEvent(Client player, string eventName, params object[] arguments)
        {
            if (eventName == "executeBankOperation")
            {
                String response = String.Empty;
                int bank = NAPI.Data.GetEntitySharedData(player, EntityData.PLAYER_BANK);
                int money = NAPI.Data.GetEntitySharedData(player, EntityData.PLAYER_MONEY);
                String name = NAPI.Data.GetEntityData(player, EntityData.PLAYER_NAME);

                // Verificamos que la cantidad es válida
                int amount = 0;

                if(Int32.TryParse(arguments[1].ToString(), out amount) == true)
                {
                    if(amount > 0)
                    {
                        switch ((int)arguments[0])
                        {
                            case Constants.OPERATION_WITHDRAW:
                                if (bank >= amount)
                                {
                                    bank -= amount;
                                    money += amount;
                                    NAPI.Data.SetEntitySharedData(player, EntityData.PLAYER_BANK, bank);
                                    NAPI.Data.SetEntitySharedData(player, EntityData.PLAYER_MONEY, money);
                                    Database.logPayment("ATM", name, "Retiro", amount);
                                }
                                else
                                {
                                    response = "La cuenta no dispone de la cantidad requerida";
                                }
                                break;
                            case Constants.OPERATION_DEPOSIT:
                                if (money >= amount)
                                {
                                    bank += amount;
                                    money -= amount;
                                    NAPI.Data.SetEntitySharedData(player, EntityData.PLAYER_BANK, bank);
                                    NAPI.Data.SetEntitySharedData(player, EntityData.PLAYER_MONEY, money);
                                    Database.logPayment(name, "ATM", "Depósito", amount);
                                }
                                else
                                {
                                    bank += money;
                                    NAPI.Data.SetEntitySharedData(player, EntityData.PLAYER_BANK, bank);
                                    NAPI.Data.SetEntitySharedData(player, EntityData.PLAYER_MONEY, 0);
                                    Database.logPayment(name, "ATM", "Depósito", money);
                                }
                                break;
                            case Constants.OPERATION_TRANSFER:
                                if (bank < amount)
                                {
                                    response = "La cuenta no dispone de la cantidad requerida";
                                }
                                else
                                {
                                    String targetName = arguments[2].ToString();
                                    if (Database.findCharacter(targetName) == true)
                                    {
                                        Client target = NAPI.Pools.GetAllPlayers().Find(x => x.Name == targetName);
                                        if(target == player)
                                        {
                                            response = "No puedes realizar una transferencia a tu cuenta";
                                        }
                                        else
                                        {
                                            if (target != null && NAPI.Data.HasEntityData(target, EntityData.PLAYER_PLAYING) == true)
                                            {
                                                int targetBank = NAPI.Data.GetEntitySharedData(target, EntityData.PLAYER_BANK);
                                                targetBank += amount;
                                                bank -= amount;
                                                NAPI.Data.SetEntitySharedData(player, EntityData.PLAYER_BANK, bank);
                                                NAPI.Data.SetEntitySharedData(target, EntityData.PLAYER_BANK, targetBank);
                                            }
                                            else
                                            {
                                                bank -= amount;
                                                NAPI.Data.SetEntitySharedData(player, EntityData.PLAYER_BANK, bank);
                                                Database.transferMoneyToPlayer(targetName, amount);
                                            }
                                            Database.logPayment(name, targetName, "Transferencia", amount);
                                        }
                                    }
                                    else
                                    {
                                        response = "La persona destinataria no tiene ninguna cuenta bancaria";
                                    }
                                }
                                break;
                            case Constants.OPERATION_BALANCE:
                                break;
                        }
                    }
                    else
                    {
                        response = "Ha habido un fallo al procesar la transacción";
                    }
                }
                else
                {
                    response = "Ha habido un fallo al procesar la transacción";
                }
                NAPI.ClientEvent.TriggerClientEvent(player, "bankOperationResponse", response);
            }
            else if(eventName == "loadPlayerBankBalance")
            {
                List<BankOperationModel> operations = Database.getBankOperations(player.Name, 1, Constants.MAX_BANK_OPERATIONS);
                NAPI.ClientEvent.TriggerClientEvent(player, "showPlayerBankBalance", NAPI.Util.ToJson(operations), player.Name);
            }
        }
    }
}
